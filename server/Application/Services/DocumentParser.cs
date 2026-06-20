using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using Application.Dtos;

namespace Application.Services;

public class DocumentParser
{
    public async Task<StrategyDto> ParseFormFileAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var tempInputFile = Path.Combine(tempDir, file.FileName);
        
        try
        {
            using (var stream = new FileStream(tempInputFile, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (extension == ".docx")
            {
                return ParseDocx(tempInputFile);
            }
            else if (extension == ".pdf")
            {
                return ParsePdf(tempInputFile);
            }
            else if (extension == ".doc")
            {
                return ParseDoc(tempInputFile, tempDir);
            }
            else
            {
                throw new InvalidDataException($"Unsupported file format: {extension}");
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }

    public StrategyDto ParseDocx(string path)
    {
        var lines = new List<string>();
        using (var doc = WordprocessingDocument.Open(path, false))
        {
            var body = doc.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                foreach (var element in body.ChildElements)
                {
                    if (element is Paragraph p)
                    {
                        var t = p.InnerText?.Trim();
                        if (!string.IsNullOrEmpty(t)) lines.Add(t);
                    }
                    else if (element is Table table)
                    {
                        if (IsTocTable(table)) continue;

                        foreach (var row in table.Elements<TableRow>())
                        {
                            foreach (var cell in row.Elements<TableCell>())
                            {
                                foreach (var cellPara in cell.Elements<Paragraph>())
                                {
                                    var cellText = cellPara.InnerText?.Trim();
                                    if (!string.IsNullOrEmpty(cellText)) lines.Add(cellText);
                                }
                            }
                        }
                    }
                }
            }
        }

        return ParseHierarchy(lines, Path.GetFileNameWithoutExtension(path), allowUnnumberedTasks: true);
    }

    private bool IsTocTable(Table table)
    {
        int tocRowsCount = 0;
        int totalRows = 0;
        foreach (var row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count < 2) continue;
            totalRows++;
            var firstCellText = cells[0].InnerText?.Trim() ?? "";
            var lastCellText = cells.Last().InnerText?.Trim() ?? "";
            
            bool isPageNumber = Regex.IsMatch(lastCellText, @"^\d{1,3}$");
            bool isTocKeyword = firstCellText.Contains("Вступ", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Зміст", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Стратегіч", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Оператив", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Операційн", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("SWOT", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Бачення", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Візія", StringComparison.OrdinalIgnoreCase) ||
                                 firstCellText.Contains("Додатки", StringComparison.OrdinalIgnoreCase);
                                 
            if (isPageNumber && isTocKeyword)
            {
                tocRowsCount++;
            }
        }
        return totalRows > 0 && ((double)tocRowsCount / totalRows) >= 0.4;
    }

    public StrategyDto ParsePdf(string path)
    {
        var lines = new List<string>();
        using (var pdf = PdfDocument.Open(path))
        {
            for (int i = 1; i <= pdf.NumberOfPages; i++)
            {
                var page = pdf.GetPage(i);
                foreach (var pageLine in ExtractPdfLines(page))
                {
                    var cleanLine = pageLine.Trim();
                    if (path.Contains("plan-zakhodiv-na-2025-2027-roki.pdf"))
                    {
                        Console.WriteLine($"[PAGE {i} LINE] '{cleanLine}'");
                    }
                    if (string.IsNullOrEmpty(cleanLine)) continue;
                    
                    // Filter headers/footers/page numbers
                    if (Regex.IsMatch(cleanLine, @"^\d+\s*$")) continue; // page number only
                    if (cleanLine.Contains("План заходів на 2025-2027 роки", StringComparison.OrdinalIgnoreCase)) continue;

                    var splitLines = PreprocessPdfLine(cleanLine);
                    lines.AddRange(splitLines);
                }
            }
        }

        return ParseHierarchy(lines, Path.GetFileNameWithoutExtension(path));
    }

    private List<string> ExtractPdfLines(UglyToad.PdfPig.Content.Page page)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0) return new List<string>();

        const double lineTolerance = 3.0;
        var rows = new List<(double Y, List<UglyToad.PdfPig.Content.Word> Words)>();

        foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom))
        {
            var row = rows.FirstOrDefault(candidate =>
                Math.Abs(candidate.Y - word.BoundingBox.Bottom) <= lineTolerance);

            if (row.Words == null)
            {
                rows.Add((word.BoundingBox.Bottom, new List<UglyToad.PdfPig.Content.Word> { word }));
            }
            else
            {
                row.Words.Add(word);
            }
        }
        rows = rows.OrderByDescending(r => r.Y).ToList();

        double gapThreshold = 12.0;
        var rowCellBlocks = new List<(List<PdfTableCell> Cells, List<UglyToad.PdfPig.Content.Word> OriginalWords)>();

        foreach (var r in rows)
        {
            var sortedWords = r.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (sortedWords.Count == 0) continue;

            var rowCells = new List<PdfTableCell>();
            var currentWords = new List<UglyToad.PdfPig.Content.Word> { sortedWords[0] };

            for (int i = 1; i < sortedWords.Count; i++)
            {
                var prev = sortedWords[i - 1];
                var curr = sortedWords[i];
                double gap = curr.BoundingBox.Left - prev.BoundingBox.Right;

                if (gap >= gapThreshold)
                {
                    rowCells.Add(new PdfTableCell(currentWords));
                    currentWords.Clear();
                }
                currentWords.Add(curr);
            }
            rowCells.Add(new PdfTableCell(currentWords));
            rowCellBlocks.Add((rowCells, r.Words));
        }

        var cellStarts = new List<double>();
        foreach (var r in rowCellBlocks)
        {
            if (r.Cells.Count >= 2)
            {
                foreach (var cell in r.Cells)
                {
                    cellStarts.Add(cell.MinX);
                }
            }
            else
            {
                foreach (var cell in r.Cells)
                {
                    double cellWidth = cell.MaxX - cell.MinX;
                    if (cellWidth < page.Width * 0.7)
                    {
                        cellStarts.Add(cell.MinX);
                    }
                }
            }
        }

        var peaks = new List<double>();
        if (cellStarts.Count > 0)
        {
            var sortedStarts = cellStarts.OrderBy(x => x).ToList();
            var currentCluster = new List<double> { sortedStarts[0] };

            for (int i = 1; i < sortedStarts.Count; i++)
            {
                if (sortedStarts[i] - sortedStarts[i - 1] < 20.0)
                {
                    currentCluster.Add(sortedStarts[i]);
                }
                else
                {
                    peaks.Add(currentCluster.Average());
                    currentCluster.Clear();
                    currentCluster.Add(sortedStarts[i]);
                }
            }
            if (currentCluster.Count > 0)
            {
                peaks.Add(currentCluster.Average());
            }
        }

        if (peaks.Count < 2)
        {
            return rows
                .Select(row => string.Join(" ", row.Words
                    .OrderBy(word => word.BoundingBox.Left)
                    .Select(word => word.Text)))
                .Select(NormalizeWhitespace)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        peaks = peaks.OrderBy(p => p).ToList();
        var boundaries = new List<double>();
        for (int i = 0; i < peaks.Count - 1; i++)
        {
            boundaries.Add((peaks[i] + peaks[i + 1]) / 2.0);
        }

        var gridRows = new List<GridRow>();

        foreach (var r in rowCellBlocks)
        {
            var rowCells = r.Cells;

            bool isSpanning = false;
            foreach (var cell in rowCells)
            {
                if (cell.MaxX - cell.MinX > page.Width * 0.45)
                {
                    isSpanning = true;
                    break;
                }
            }

            if (isSpanning)
            {
                var flatLine = string.Join(" ", r.OriginalWords
                    .OrderBy(w => w.BoundingBox.Left)
                    .Select(w => w.Text));
                gridRows.Add(new GridRow { IsSpanning = true, FlatText = flatLine });
            }
            else
            {
                var rowTexts = new string[peaks.Count];
                for (int i = 0; i < peaks.Count; i++) rowTexts[i] = "";

                foreach (var cell in rowCells)
                {
                    int colIdx = 0;
                    while (colIdx < boundaries.Count && cell.MinX > boundaries[colIdx])
                    {
                        colIdx++;
                    }

                    if (rowTexts[colIdx] != "") rowTexts[colIdx] += " ";
                    rowTexts[colIdx] += cell.Text;
                }

                if (rowTexts.Any(t => !string.IsNullOrEmpty(t)))
                {
                    gridRows.Add(new GridRow { IsSpanning = false, Cells = rowTexts });
                }
            }
        }

        var sgRegex = new Regex(@"^\s*(?:Стратегічна ціль|СТРАТЕГІЧНА ЦІЛЬ|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[I|V|X]+)", RegexOptions.IgnoreCase);
        var ogRegex = new Regex(@"^\s*(?:Операційна ціль|Оперативна ціль|Ціль)\s+(\d+\.\d+)", RegexOptions.IgnoreCase);
        var ogNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+)(?!\.\d)\b");
        var taskRegex = new Regex(@"^\s*(?:Завдання|Захід|Проект|Проєкт)\s+(\d+\.\d+\.\d+|\d+)", RegexOptions.IgnoreCase);
        var taskNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+\.\d+(?:\.\d+)*)\b");

        // Pre-merge column-based vertical cell continuations
        if (peaks.Count >= 2)
        {
            var lastNonEmptyRow = new int[peaks.Count];
            for (int c = 0; c < peaks.Count; c++) lastNonEmptyRow[c] = -1;

            for (int rIndex = 0; rIndex < gridRows.Count; rIndex++)
            {
                var gridRow = gridRows[rIndex];
                if (gridRow.IsSpanning || gridRow.Cells == null) continue;

                for (int c = 0; c < peaks.Count; c++)
                {
                    var cellText = gridRow.Cells[c].Trim();
                    if (string.IsNullOrEmpty(cellText)) continue;

                    int prevR = lastNonEmptyRow[c];
                    if (prevR >= 0)
                    {
                        var prevText = gridRows[prevR].Cells![c];
                        
                        bool startsNewItem = false;
                        if (c == 0)
                        {
                            startsNewItem = sgRegex.IsMatch(cellText) || 
                                            ogRegex.IsMatch(cellText) || 
                                            ogNumberOnlyRegex.IsMatch(cellText) ||
                                            taskRegex.IsMatch(cellText) ||
                                            taskNumberOnlyRegex.IsMatch(cellText);
                        }
                        else if (c == 1)
                        {
                            startsNewItem = taskRegex.IsMatch(cellText) || 
                                            taskNumberOnlyRegex.IsMatch(cellText) ||
                                            sgRegex.IsMatch(cellText) || 
                                            ogRegex.IsMatch(cellText);
                        }

                        bool isCont = false;
                        if (!startsNewItem)
                        {
                            isCont = char.IsLower(cellText[0]) || 
                                     (prevText.Length > 0 && !".!?;;".Contains(prevText.TrimEnd().Last()) && !Regex.IsMatch(cellText, @"^\d"));
                        }

                        if (isCont)
                        {
                            gridRows[prevR].Cells[c] = (prevText + " " + cellText).Trim();
                            gridRow.Cells[c] = "";
                        }
                        else
                        {
                            lastNonEmptyRow[c] = rIndex;
                        }
                    }
                    else
                    {
                        lastNonEmptyRow[c] = rIndex;
                    }
                }
            }
        }

        var mergedLines = new List<string>();
        string[] currentMergedRow = null;

        bool StartsNewRow(string[] row)
        {
            var col0 = row[0].Trim();
            var col1 = row.Length > 1 ? row[1].Trim() : "";

            if (sgRegex.IsMatch(col0) || ogRegex.IsMatch(col0) || ogNumberOnlyRegex.IsMatch(col0) ||
                taskRegex.IsMatch(col0) || taskNumberOnlyRegex.IsMatch(col0))
            {
                return true;
            }

            if (sgRegex.IsMatch(col1) || ogRegex.IsMatch(col1) || ogNumberOnlyRegex.IsMatch(col1) ||
                taskRegex.IsMatch(col1) || taskNumberOnlyRegex.IsMatch(col1))
            {
                return true;
            }

            return false;
        }

        void OutputCurrentMergedRow()
        {
            if (currentMergedRow != null)
            {
                foreach (var cellText in currentMergedRow)
                {
                    var trimmed = cellText.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        mergedLines.Add(trimmed);
                    }
                }
                currentMergedRow = null;
            }
        }

        foreach (var gridRow in gridRows)
        {
            if (gridRow.IsSpanning)
            {
                OutputCurrentMergedRow();
                if (gridRow.FlatText != null) mergedLines.Add(gridRow.FlatText);
            }
            else
            {
                var rowTexts = gridRow.Cells;
                if (rowTexts == null) continue;

                if (StartsNewRow(rowTexts))
                {
                    OutputCurrentMergedRow();
                    currentMergedRow = (string[])rowTexts.Clone();
                }
                else
                {
                    if (currentMergedRow == null)
                    {
                        currentMergedRow = (string[])rowTexts.Clone();
                    }
                    else
                    {
                        for (int i = 0; i < Math.Min(currentMergedRow.Length, rowTexts.Length); i++)
                        {
                            if (!string.IsNullOrEmpty(rowTexts[i]))
                            {
                                if (currentMergedRow[i] != "") currentMergedRow[i] += " ";
                                currentMergedRow[i] += rowTexts[i];
                            }
                        }
                    }
                }
            }
        }
        OutputCurrentMergedRow();

        return mergedLines;
    }

    public StrategyDto ParseDoc(string path, string tempDir)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "soffice",
                Arguments = $"--headless --convert-to docx \"{path}\" --outdir \"{tempDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        var docxFiles = Directory.GetFiles(tempDir, "*.docx");
        if (docxFiles.Length == 0)
        {
            throw new InvalidDataException("Failed to convert legacy .doc file to .docx via LibreOffice.");
        }

        return ParseDocx(docxFiles[0]);
    }

    private List<string> PreprocessPdfLine(string line)
    {
        var result = new List<string>();
        var splitRegex = new Regex(@"\s+(?=(?:Стратегічна ціль|СТРАТЕГІЧНА ЦІЛЬ|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(?:\d+|[I|V|X]+)|(?:Операційна ціль|Оперативна ціль|Ціль)\s+\d+\.\d+|\b\d+\.\d+(?!\.\d)\b|Завдання|Захід|Проект|Проєкт|\b\d+\.\d+\.\d+\b)", RegexOptions.IgnoreCase);
        var parts = splitRegex.Split(line);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                result.Add(trimmed);
            }
        }
        return result;
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private string CleanTaskDescription(string desc)
    {
        var clean = Regex.Replace(desc, @"\s+\d+[\s\d]*(?:,\d{2})?\s*$", "");
        return clean.Trim();
    }

    public StrategyDto ParseHierarchy(
        List<string> lines,
        string filename,
        bool allowUnnumberedTasks = false)
    {
        var strategyTitle = ExtractStrategyTitle(lines, filename);
        lines = RemoveLeadingTableOfContents(lines);

        var strategy = new StrategyDto
        {
            Title = strategyTitle
        };

        var strategicGoals = new List<StrategicGoalDto>();
        StrategicGoalDto? currentSG = null;
        OperationalGoalDto? currentOG = null;
        bool isInTasksSection = false;

        var structureStrategicGoals = new HashSet<StrategicGoalDto>();
        var structureOperationalGoals = new HashSet<OperationalGoalDto>();
        var structureTasks = new HashSet<ProgramTaskDto>();
        bool isInStructureSection = false;
        string activeStructureTableNumber = "";

        var structureHeaderRegex = new Regex(@"(?:Структура|Матриця|Дерево|Система)\s+(?:стратегічних|оперативних|цілей|завдань|напрямів)", RegexOptions.IgnoreCase);
        var structureEndRegex = new Regex(@"^(Фінансове|Моніторинг|Реалізація|Впровадження|РОЗДІЛ\s+[I|V|X|L|\d]+|Розділ\s+[I|V|X|L|\d]+)", RegexOptions.IgnoreCase);

        // Regex patterns with ^ anchors and lookaheads
        var sgRegex = new Regex(@"^\s*(?:Стратегічна ціль|СТРАТЕГІЧНА ЦІЛЬ|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[I|V|X]+)[\.:\s]*(?!\p{Ll})\s*(.*)$", RegexOptions.IgnoreCase);
        var ogRegex = new Regex(@"^\s*(?:Операційна ціль|Оперативна ціль|Ціль)\s+(\d+\.\d+)[\.:\s]*(?!\p{Ll})\s*(.*)$", RegexOptions.IgnoreCase);
        var ogNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+)(?!\.\d)\b\.?\s*(?!\p{Ll})(.*)$");
        
        var taskRegex = new Regex(@"^\s*(?:Завдання|Захід|Проект|Проєкт)\s+(\d+\.\d+\.\d+|\d+)[\.:\s]*\s*(.*)$", RegexOptions.IgnoreCase);
        var taskNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+\.\d+(?:\.\d+)*)\b\.?\s*(.*)$");
        var tasksHeaderRegex = new Regex(@"^(Заходи/проєкти|Заходи|Проекти/заходи|Завдання/заходи|Перелік заходів|Завдання):?\s*$", RegexOptions.IgnoreCase);
        var skipTaskKeywords = new Regex(@"^(Бенефіціари|Обґрунтування|Мета|Термін|Всього|Таблиця|Назва проекту|Період реалізації)", RegexOptions.IgnoreCase);
        var strategySectionRegex = new Regex(
            @"^(Пріоритети розвитку|Реалізація та моніторинг|Управління процесом реалізації|Заключні положення|Відповідність стратегії|Додаток|Оцінка можливого впливу стратегії|Показники оцінки реалізації|Індикатори досягнення|Загальні показники|Моніторинг|Рівні моніторингу|У ході моніторингу)",
            RegexOptions.IgnoreCase);

        // Common document sections to ignore as false positive goals
        var docSectionRegex = new Regex(@"^(Вступ|Коротка характеристика|Загальна інформація|Природні ресурси|Історична довідка|Соціальна сфера|Житлово-комунальне|Економіка та бюджет|Методологія|Аналіз|Місія та стратегічне|SWOT|Впровадження|Моніторинг|Фінансове забезпечення|Система моніторингу|Мережа закладів|Мережа медичних|Промисловість|Лісове господарство|Сільське господарство|Перелік умовних|Паспорт громади|Зміст|Розділ|Таблиця)", RegexOptions.IgnoreCase);
        
        // TOC leader regex to filter out TOC entries containing leader lines (e.g. dots or underscores followed by page number)
        var tocLeaderRegex = new Regex(@"[\._~-]{3,}\s*\d+\s*$", RegexOptions.IgnoreCase);

        bool IsGoalOrTaskLine(string l)
        {
            var t = l.Trim();
            return sgRegex.IsMatch(t) || 
                   ogRegex.IsMatch(t) || 
                   ogNumberOnlyRegex.IsMatch(t) || 
                   taskRegex.IsMatch(t) || 
                   taskNumberOnlyRegex.IsMatch(t);
        }

        bool IsLikelyGoalContinuation(string currentTitle, string nextLine)
        {
            if (string.IsNullOrEmpty(currentTitle) || string.IsNullOrEmpty(nextLine)) return false;
            
            var trimmedNext = nextLine.Trim();
            if (trimmedNext.Length == 0) return false;

            if (IsGoalOrTaskLine(trimmedNext) || tocLeaderRegex.IsMatch(trimmedNext)) return false;
            
            var lineWithoutNumber = Regex.Replace(trimmedNext, @"^\s*\d+(?:\.\d+)*\.?\s*", "");
            if (strategySectionRegex.IsMatch(lineWithoutNumber) || docSectionRegex.IsMatch(lineWithoutNumber)) return false;

            if (char.IsLower(trimmedNext[0])) return true;

            var lastWordMatch = Regex.Match(currentTitle, @"\b(та|і|й|а|але|чи|або|з|в|у|на|для|під|над|по|також)\s*$", RegexOptions.IgnoreCase);
            if (lastWordMatch.Success) return true;

            return false;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmedLine = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            
            // Skip lines that are clearly from Table of Contents (have leader characters + page numbers at the end)
            if (tocLeaderRegex.IsMatch(trimmedLine)) continue;

            if (structureHeaderRegex.IsMatch(trimmedLine))
            {
                isInStructureSection = true;
                Console.WriteLine($"[DEBUG] ENTER structure section on line: {trimmedLine}");
                
                var tableNumMatch = Regex.Match(trimmedLine, @"Таблиця\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (tableNumMatch.Success)
                {
                    activeStructureTableNumber = tableNumMatch.Groups[1].Value;
                    Console.WriteLine($"[DEBUG] Detected structure table number: {activeStructureTableNumber}");
                }
                else
                {
                    activeStructureTableNumber = "";
                }
            }
            else if (isInStructureSection)
            {
                bool shouldExit = false;
                var tableMatch = Regex.Match(trimmedLine, @"^Таблиця\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (tableMatch.Success)
                {
                    var tableNum = tableMatch.Groups[1].Value;
                    if (tableNum != activeStructureTableNumber)
                    {
                        shouldExit = true;
                    }
                }
                else if (structureEndRegex.IsMatch(trimmedLine))
                {
                    shouldExit = true;
                }

                if (shouldExit)
                {
                    isInStructureSection = false;
                    activeStructureTableNumber = "";
                    Console.WriteLine($"[DEBUG] EXIT structure section on line: {trimmedLine}");
                }
            }

            var lineWithoutNumber = Regex.Replace(
                trimmedLine,
                @"^\s*\d+(?:\.\d+)*\.?\s*",
                "");
            if (!structureHeaderRegex.IsMatch(trimmedLine) && (strategySectionRegex.IsMatch(lineWithoutNumber) || docSectionRegex.IsMatch(lineWithoutNumber)))
            {
                currentSG = null;
                currentOG = null;
                isInTasksSection = false;
                isInStructureSection = false;
                continue;
            }

            // 1. Strategic Goal Match
            var sgMatch = sgRegex.Match(trimmedLine);
            if (sgMatch.Success)
            {
                var label = sgMatch.Groups[1].Value.Trim();
                var title = sgMatch.Groups[2].Value.Trim();

                // Multiline header support: look ahead if title is empty or next line is a continuation
                while (i + 1 < lines.Count)
                {
                    var nextLine = lines[i + 1].Trim();
                    if (string.IsNullOrEmpty(title))
                    {
                        if (!IsGoalOrTaskLine(nextLine) && !tocLeaderRegex.IsMatch(nextLine) && nextLine.Length > 0)
                        {
                            title = nextLine;
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (IsLikelyGoalContinuation(title, nextLine))
                    {
                        title = JoinTaskText(title, nextLine);
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Semantic filter: Ignore section headers
                if (docSectionRegex.IsMatch(title)) continue;
                
                // Parse label to number
                int num = 0;
                int.TryParse(label, out num);
                if (num == 0 && label.Length > 0)
                {
                    // Fallback Roman numerals
                    num = ParseRomanNumeral(label);
                }

                currentSG = new StrategicGoalDto
                {
                    Label = label,
                    Number = num > 0 ? num : (strategicGoals.Count + 1),
                    Title = title
                };
                strategicGoals.Add(currentSG);
                if (isInStructureSection)
                {
                    structureStrategicGoals.Add(currentSG);
                }
                currentOG = null;
                isInTasksSection = false;
                continue;
            }

            // 2. Operational Goal Match
            var ogMatch = ogRegex.Match(trimmedLine);
            string ogLabel = "";
            string ogTitle = "";
            bool isOg = false;

            if (ogMatch.Success)
            {
                ogLabel = ogMatch.Groups[1].Value.Trim();
                ogTitle = ogMatch.Groups[2].Value.Trim();
                isOg = true;
            }
            else
            {
                var ogNumMatch = ogNumberOnlyRegex.Match(trimmedLine);
                if (ogNumMatch.Success)
                {
                    ogLabel = ogNumMatch.Groups[1].Value.Trim();
                    ogTitle = ogNumMatch.Groups[2].Value.Trim();
                    // Make sure it's not a task number
                    if (ogLabel.Contains(".")) 
                    {
                        var parts = ogLabel.Split('.');
                        if (parts.Length == 2)
                        {
                            isOg = true;
                        }
                    }
                }
            }

            if (isOg)
            {
                // Multiline header support: look ahead if title is empty or next line is a continuation
                while (i + 1 < lines.Count)
                {
                    var nextLine = lines[i + 1].Trim();
                    if (string.IsNullOrEmpty(ogTitle))
                    {
                        if (!IsGoalOrTaskLine(nextLine) && !tocLeaderRegex.IsMatch(nextLine) && nextLine.Length > 0)
                        {
                            ogTitle = nextLine;
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (IsLikelyGoalContinuation(ogTitle, nextLine))
                    {
                        ogTitle = JoinTaskText(ogTitle, nextLine);
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Semantic filter: Ignore section headers
                if (docSectionRegex.IsMatch(ogTitle) || tasksHeaderRegex.IsMatch(ogTitle)) continue;
                if (!IsPlausibleGoalTitle(ogTitle)) continue;

                var parts = ogLabel.Split('.');
                var parentSgLabel = parts[0];

                // Only parse if we have a matching strategic goal registered!
                var matchingSg = strategicGoals.FirstOrDefault(g => g.Label.Equals(parentSgLabel, StringComparison.OrdinalIgnoreCase));
                if (matchingSg != null)
                {
                    currentSG = matchingSg;
                    int num = 1;
                    int.TryParse(parts[1], out num);

                    currentOG = new OperationalGoalDto
                    {
                        Label = ogLabel,
                        Number = num,
                        Title = ogTitle
                    };
                    currentSG.OperationalGoals.Add(currentOG);
                    if (isInStructureSection)
                    {
                        structureOperationalGoals.Add(currentOG);
                    }
                    isInTasksSection = false;
                }
                continue;
            }

            // 3. Program Task Match
            var taskMatch = taskRegex.Match(trimmedLine);
            var taskNumMatch = taskNumberOnlyRegex.Match(trimmedLine);

            bool isTask = false;
            string taskLabel = "";
            string taskDesc = "";

            if (taskMatch.Success)
            {
                taskLabel = taskMatch.Groups[1].Value.Trim();
                taskDesc = CleanTaskDescription(taskMatch.Groups[2].Value.Trim());
                isTask = true;
            }
            else if (taskNumMatch.Success)
            {
                taskLabel = taskNumMatch.Groups[1].Value.Trim();
                taskDesc = CleanTaskDescription(taskNumMatch.Groups[2].Value.Trim());
                isTask = true;
            }
            if (isTask && tasksHeaderRegex.IsMatch(taskDesc))
            {
                continue;
            }

            if (isTask && string.IsNullOrWhiteSpace(taskDesc))
            {
                continue;
            }

            if (isTask)
            {
                var parts = taskLabel.Split('.');
                if (parts.Length >= 2)
                {
                    var parentSgLabel = parts[0];
                    var parentOgLabel = string.Join(".", parts.Take(parts.Length - 1));

                    var matchingSg = strategicGoals.FirstOrDefault(g => g.Label.Equals(parentSgLabel, StringComparison.OrdinalIgnoreCase));
                    if (matchingSg == null)
                    {
                        int num = 0;
                        int.TryParse(parentSgLabel, out num);
                        if (num == 0) num = ParseRomanNumeral(parentSgLabel);
                        matchingSg = new StrategicGoalDto
                        {
                            Label = parentSgLabel,
                            Number = num > 0 ? num : (strategicGoals.Count + 1),
                            Title = ""
                        };
                        strategicGoals.Add(matchingSg);
                        if (isInStructureSection)
                        {
                            structureStrategicGoals.Add(matchingSg);
                        }
                    }

                    var matchingOg = matchingSg.OperationalGoals.FirstOrDefault(g => g.Label.Equals(parentOgLabel, StringComparison.OrdinalIgnoreCase));
                    if (matchingOg == null)
                    {
                        int num = 1;
                        if (parts.Length > 1) int.TryParse(parts[1], out num);
                        matchingOg = new OperationalGoalDto
                        {
                            Label = parentOgLabel,
                            Number = num,
                            Title = ""
                        };
                        matchingSg.OperationalGoals.Add(matchingOg);
                        if (isInStructureSection)
                        {
                            structureOperationalGoals.Add(matchingOg);
                        }
                    }

                    currentSG = matchingSg;
                    currentOG = matchingOg;
                }
            }

            if (isTask && currentOG != null)
            {
                var parts = taskLabel.Split('.');
                bool isParentMatch = false;
                if (parts.Length >= 2)
                {
                    var parentLabel = string.Join(".", parts.Take(parts.Length - 1));
                    if (parentLabel.Equals(currentOG.Label, StringComparison.OrdinalIgnoreCase))
                    {
                        isParentMatch = true;
                    }
                    else if (allowUnnumberedTasks &&
                             parts.Length == 3 &&
                             parts[0].Equals(currentSG?.Label, StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(parts[2], out var taskNumber) &&
                             taskNumber == currentOG.ProgramTasks.Count + 1)
                    {
                        taskLabel = $"{currentOG.Label}.{parts[2]}";
                        parts = taskLabel.Split('.');
                        isParentMatch = true;
                    }
                }
                else
                {
                    isParentMatch = true; 
                }

                if (isParentMatch)
                {
                    int num = currentOG.ProgramTasks.Count + 1;
                    if (parts.Length > 0)
                    {
                        int.TryParse(parts.Last(), out num);
                    }

                    if (num <= 0 || (!allowUnnumberedTasks && num > 150) || num >= 1900)
                    {
                        continue;
                    }

                    var previousTask = currentOG.ProgramTasks.LastOrDefault();
                    if (previousTask != null && IsLikelyTaskContinuation(taskDesc))
                    {
                        previousTask.Description = JoinTaskText(previousTask.Description, taskDesc);
                        continue;
                    }

                    var task = new ProgramTaskDto
                    {
                        Label = taskLabel.Contains(".") ? taskLabel : $"{currentOG.Label}.{taskLabel}",
                        Number = num,
                        Description = taskDesc
                    };
                    currentOG.ProgramTasks.Add(task);
                    if (isInStructureSection)
                    {
                        structureTasks.Add(task);
                        Console.WriteLine($"[DEBUG] Added structure task: {task.Label} - {task.Description}");
                    }
                }
                continue;
            }

            // 4. Tasks Section Header
            if (tasksHeaderRegex.IsMatch(trimmedLine))
            {
                isInTasksSection = true;
                continue;
            }

            // 5. Continue the preceding numbered task across wrapped lines.
            if ((isInTasksSection || isInStructureSection) && currentOG != null)
            {
                if (skipTaskKeywords.IsMatch(trimmedLine)) continue;
                if (trimmedLine.Length < 5) continue; // too short to be a task

                // Check if it looks like a new header or page break
                if (trimmedLine.Contains("Стратегічна ціль", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Операційна ціль", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Оперативна ціль", StringComparison.OrdinalIgnoreCase))
                {
                    isInTasksSection = false;
                    continue;
                }

                var cleanDesc = Regex.Replace(trimmedLine, @"^[\-\*\•]+\s*", "").Trim();
                cleanDesc = CleanTaskDescription(cleanDesc);

                var previousTask = currentOG.ProgramTasks.LastOrDefault();
                if (previousTask != null &&
                    !string.IsNullOrEmpty(cleanDesc) &&
                    IsLikelyTaskContinuation(cleanDesc))
                {
                    previousTask.Description = JoinTaskText(previousTask.Description, cleanDesc);
                }
                else if (allowUnnumberedTasks &&
                          !string.IsNullOrEmpty(cleanDesc) &&
                          !skipTaskKeywords.IsMatch(cleanDesc) &&
                          !docSectionRegex.IsMatch(cleanDesc))
                {
                    var taskIndex = currentOG.ProgramTasks.Count + 1;
                    var task = new ProgramTaskDto
                    {
                        Label = $"{currentOG.Label}.{taskIndex}",
                        Number = taskIndex,
                        Description = cleanDesc
                    };
                    currentOG.ProgramTasks.Add(task);
                    if (isInStructureSection)
                    {
                        structureTasks.Add(task);
                    }
                }
            }
        }

        // Consolidate repeated goals from contents, summaries and main sections.
        foreach (var sg in strategicGoals)
        {
            sg.OperationalGoals = sg.OperationalGoals
                .GroupBy(og => og.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => {
                    if (structureOperationalGoals.Count > 0 && !g.Any(x => structureOperationalGoals.Contains(x)))
                    {
                        return null;
                    }

                    var primary = g
                        .OrderBy(x => {
                            bool isStruct = structureOperationalGoals.Contains(x);
                            return (isStruct ? 0 : 5000) + GoalTitleScore(x.Title);
                        })
                        .First();

                    var bestTitle = g
                        .Select(x => x.Title)
                        .Where(t => !string.IsNullOrEmpty(t) && t.Length < 250)
                        .OrderByDescending(t => t.Length)
                        .FirstOrDefault() ?? primary.Title;
                    primary.Title = bestTitle;

                    var allTasks = g
                        .SelectMany(x => x.ProgramTasks)
                        .GroupBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
                        .Select(gt => {
                            var structTasksInGroup = gt
                                .Where(task => structureTasks.Contains(task))
                                .ToList();
                            if (structureTasks.Count > 0 && structTasksInGroup.Count == 0)
                            {
                                return null;
                            }
                            return structTasksInGroup.Count > 0
                                ? structTasksInGroup.OrderByDescending(t => t.Description.Length).First()
                                : gt.OrderByDescending(t => t.Description.Length).First();
                        })
                        .Where(t => t != null)
                        .Select(t => t!)
                        .OrderBy(t => t.Number)
                        .ToList();
                    primary.ProgramTasks = allTasks;
                    return primary;
                })
                .Where(og => og != null)
                .Select(og => og!)
                .Where(og => og.ProgramTasks.Count > 0)
                .ToList();
        }

        strategy.StrategicGoals = strategicGoals
            .GroupBy(sg => sg.Label, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                if (structureStrategicGoals.Count > 0 && !group.Any(x => structureStrategicGoals.Contains(x)))
                {
                    return null;
                }

                var primary = group
                    .OrderBy(goal => {
                        bool isStruct = structureStrategicGoals.Contains(goal);
                        return (isStruct ? 0 : 5000) + GoalTitleScore(goal.Title);
                    })
                    .First();

                var bestTitle = group
                    .Select(x => x.Title)
                    .Where(t => !string.IsNullOrEmpty(t) && t.Length < 250)
                    .OrderByDescending(t => t.Length)
                    .FirstOrDefault() ?? primary.Title;
                primary.Title = bestTitle;

                primary.OperationalGoals = group
                    .SelectMany(goal => goal.OperationalGoals)
                    .GroupBy(goal => goal.Label, StringComparer.OrdinalIgnoreCase)
                    .Select(operationalGroup =>
                    {
                        if (structureOperationalGoals.Count > 0 && !operationalGroup.Any(x => structureOperationalGoals.Contains(x)))
                        {
                            return null;
                        }

                        var operationalPrimary = operationalGroup
                            .OrderBy(goal => {
                                bool isStruct = structureOperationalGoals.Contains(goal);
                                return (isStruct ? 0 : 5000) + GoalTitleScore(goal.Title);
                            })
                            .First();

                        var bestOpTitle = operationalGroup
                            .Select(x => x.Title)
                            .Where(t => !string.IsNullOrEmpty(t) && t.Length < 250)
                            .OrderByDescending(t => t.Length)
                            .FirstOrDefault() ?? operationalPrimary.Title;
                        operationalPrimary.Title = bestOpTitle;

                        operationalPrimary.ProgramTasks = operationalGroup
                            .SelectMany(goal => goal.ProgramTasks)
                            .GroupBy(task => task.Label, StringComparer.OrdinalIgnoreCase)
                            .Select(taskGroup => {
                                var structTasksInGroup = taskGroup
                                    .Where(task => structureTasks.Contains(task))
                                    .ToList();
                                if (structureTasks.Count > 0 && structTasksInGroup.Count == 0)
                                {
                                    return null;
                                }
                                return structTasksInGroup.Count > 0
                                    ? structTasksInGroup.OrderByDescending(t => t.Description.Length).First()
                                    : taskGroup.OrderByDescending(task => task.Description.Length).First();
                            })
                            .Where(task => task != null)
                            .Select(task => task!)
                            .OrderBy(task => task.Number)
                            .ToList();
                        return operationalPrimary;
                    })
                    .Where(goal => goal != null)
                    .Select(goal => goal!)
                    .OrderBy(goal => goal.Number)
                    .ToList();
                return primary;
            })
            .Where(sg => sg != null)
            .Select(sg => sg!)
            .Where(sg => sg.OperationalGoals.Count > 0)
            .OrderBy(sg => sg.Number)
            .ToList();
        return strategy;
    }

    private bool IsPlausibleGoalTitle(string title)
    {
        var normalized = NormalizeWhitespace(title);
        if (normalized.Length < 3 || normalized.Length > 500) return false;
        if (!normalized.Any(char.IsLetter)) return false;
        if (Regex.IsMatch(normalized, @"^[\)\]\.,;:+\-=*]+")) return false;

        var digitCount = normalized.Count(char.IsDigit);
        return digitCount <= Math.Max(8, normalized.Length / 5);
    }

    private int GoalTitleScore(string title)
    {
        var normalized = NormalizeWhitespace(title);
        var score = normalized.Length;
        score += normalized.Count(char.IsDigit) * 20;
        if (normalized.Contains("Таблиця", StringComparison.OrdinalIgnoreCase)) score += 1000;
        if (normalized.Contains("Рис.", StringComparison.OrdinalIgnoreCase)) score += 1000;
        if (normalized.Length > 250) score += 1000;
        if (normalized.Length < 15) score += 300;
        if (normalized.Length > 0 && char.IsLower(normalized[0])) score += 500;
        score += normalized.Count(character => character is '*' or '+') * 100;
        return score;
    }

    private List<string> RemoveLeadingTableOfContents(List<string> lines)
    {
        var tocIndex = lines.FindIndex(line =>
            Regex.IsMatch(line.Trim(), @"^Зміст\s*$", RegexOptions.IgnoreCase));
        if (tocIndex < 0)
        {
            return lines;
        }

        var tocGoalRegex = new Regex(
            @"^\s*(?:Стратегічна ціль|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[IVX]+)\b.*?(?:[\._~-]{3,}|\s+\d+\s*$)",
            RegexOptions.IgnoreCase);

        var bodyGoalRegex = new Regex(
            @"^\s*(?:Стратегічна ціль|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[IVX]+)\b",
            RegexOptions.IgnoreCase);

        var firstGoalIndex = -1;
        var firstGoalLabel = "";
        var searchLimit = Math.Min(lines.Count, tocIndex + 150);
        for (var i = tocIndex + 1; i < searchLimit; i++)
        {
            var match = tocGoalRegex.Match(lines[i]);
            if (!match.Success) continue;

            firstGoalIndex = i;
            firstGoalLabel = match.Groups[1].Value;
            break;
        }

        if (firstGoalIndex < 0)
        {
            return lines;
        }

        for (var i = firstGoalIndex + 1; i < lines.Count; i++)
        {
            var match = bodyGoalRegex.Match(lines[i]);
            if (match.Success &&
                match.Groups[1].Value.Equals(firstGoalLabel, StringComparison.OrdinalIgnoreCase))
            {
                return lines.Skip(i).ToList();
            }
        }

        return lines;
    }

    private bool IsLikelyTaskContinuation(string text)
    {
        var trimmed = text.TrimStart();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        return char.IsLower(trimmed[0]) ||
               trimmed[0] is '(' or '[' ||
               trimmed.StartsWith("«", StringComparison.Ordinal);
    }

    private string JoinTaskText(string first, string continuation)
    {
        var left = first.TrimEnd();
        var right = continuation.Trim();
        if (string.IsNullOrEmpty(left)) return right;
        if (string.IsNullOrEmpty(right)) return left;

        return $"{left} {right}";
    }

    public string ExtractStrategyTitle(List<string> lines, string fallback)
    {
        for (int i = 0; i < Math.Min(30, lines.Count); i++)
        {
            var line = lines[i].Trim();
            if (line.Contains("СТРАТЕГІЯ", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("ПЛАН ЗАХОДІВ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Стратегія розвитку", StringComparison.OrdinalIgnoreCase))
            {
                var titleParts = new List<string>();
                int gathered = 0;
                for (int j = i; j < Math.Min(lines.Count, i + 5); j++)
                {
                    var part = lines[j].Trim();
                    if (string.IsNullOrEmpty(part)) continue;
                    if (part.StartsWith("на ", StringComparison.OrdinalIgnoreCase) || 
                        part.StartsWith("до ", StringComparison.OrdinalIgnoreCase) || 
                        part.Contains("роки", StringComparison.OrdinalIgnoreCase) || 
                        part.Contains("громади", StringComparison.OrdinalIgnoreCase) ||
                        part.Contains("СТРАТЕГІЯ", StringComparison.OrdinalIgnoreCase) ||
                        part.Contains("ПЛАН ЗАХОДІВ", StringComparison.OrdinalIgnoreCase) ||
                        part.Contains("Стратегія розвитку", StringComparison.OrdinalIgnoreCase))
                    {
                        titleParts.Add(part);
                        if (++gathered >= 3) break;
                    }
                    else if (gathered > 0)
                    {
                        break;
                    }
                }
                if (titleParts.Count > 0)
                {
                    return string.Join(" ", titleParts);
                }
            }
        }
        return fallback;
    }

    private int ParseRomanNumeral(string roman)
    {
        roman = roman.ToUpper().Trim();
        if (roman == "I") return 1;
        if (roman == "II") return 2;
        if (roman == "III") return 3;
        if (roman == "IV") return 4;
        if (roman == "V") return 5;
        if (roman == "VI") return 6;
        if (roman == "VII") return 7;
        if (roman == "VIII") return 8;
        if (roman == "IX") return 9;
        if (roman == "X") return 10;
        return 0;
    }

    private class GridRow
    {
        public bool IsSpanning { get; set; }
        public string? FlatText { get; set; }
        public string[]? Cells { get; set; }
    }

    private class PdfTableCell
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public string Text { get; set; }

        public PdfTableCell(List<UglyToad.PdfPig.Content.Word> words)
        {
            MinX = words.Min(w => w.BoundingBox.Left);
            MaxX = words.Max(w => w.BoundingBox.Right);
            Text = string.Join(" ", words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
        }
    }
}
