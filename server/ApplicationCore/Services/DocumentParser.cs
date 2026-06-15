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
using ApplicationCore.Dtos;

namespace ApplicationCore.Services;

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

        var lefts = words.Select(w => w.BoundingBox.Left).ToList();
        var bins = lefts.GroupBy(x => Math.Round(x / 10.0) * 10.0)
                       .Select(g => new { Bin = g.Key, Count = g.Count() })
                       .OrderBy(g => g.Bin)
                       .ToList();

        var peaks = new List<double>();
        for (int i = 0; i < bins.Count; i++)
        {
            var bin = bins[i];
            if (bin.Count >= 5)
            {
                bool isLocalMax = true;
                if (i > 0 && bins[i - 1].Count > bin.Count) isLocalMax = false;
                if (i < bins.Count - 1 && bins[i + 1].Count > bin.Count) isLocalMax = false;
                if (isLocalMax)
                {
                    peaks.Add(bin.Bin);
                }
            }
        }

        if (peaks.Count < 3)
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

        double gapThreshold = 12.0;
        var gridRows = new List<GridRow>();

        foreach (var row in rows)
        {
            var sortedWords = row.Words.OrderBy(w => w.BoundingBox.Left).ToList();
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
                var flatLine = string.Join(" ", row.Words
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

        var mergedLines = new List<string>();
        string[] currentMergedRow = null;

        var sgRegex = new Regex(@"^\s*(?:Стратегічна ціль|СТРАТЕГІЧНА ЦІЛЬ|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[I|V|X]+)", RegexOptions.IgnoreCase);
        var ogRegex = new Regex(@"^\s*(?:Операційна ціль|Оперативна ціль|Ціль)\s+(\d+\.\d+)", RegexOptions.IgnoreCase);
        var ogNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+)(?!\.\d)\b");
        var taskRegex = new Regex(@"^\s*(?:Завдання|Захід|Проект|Проєкт)\s+(\d+\.\d+\.\d+|\d+)", RegexOptions.IgnoreCase);
        var taskNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+\.\d+(?:\.\d+)*)\b");

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

        // Regex patterns with ^ anchors and lookaheads
        var sgRegex = new Regex(@"^\s*(?:Стратегічна ціль|СТРАТЕГІЧНА ЦІЛЬ|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[I|V|X]+)[\.:\s]*\s*(.*)$", RegexOptions.IgnoreCase);
        var ogRegex = new Regex(@"^\s*(?:Операційна ціль|Оперативна ціль|Ціль)\s+(\d+\.\d+)[\.:\s]*\s*(.*)$", RegexOptions.IgnoreCase);
        var ogNumberOnlyRegex = new Regex(@"^\s*(\d+\.\d+)(?!\.\d)\b\.?\s*(.*)$");
        
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

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmedLine = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            
            // Skip lines that are clearly from Table of Contents (have leader characters + page numbers at the end)
            if (tocLeaderRegex.IsMatch(trimmedLine)) continue;

            var lineWithoutNumber = Regex.Replace(
                trimmedLine,
                @"^\s*\d+(?:\.\d+)*\.?\s*",
                "");
            if (strategySectionRegex.IsMatch(lineWithoutNumber) || docSectionRegex.IsMatch(lineWithoutNumber))
            {
                currentSG = null;
                currentOG = null;
                isInTasksSection = false;
                continue;
            }

            // 1. Strategic Goal Match
            var sgMatch = sgRegex.Match(trimmedLine);
            if (sgMatch.Success)
            {
                var label = sgMatch.Groups[1].Value.Trim();
                var title = sgMatch.Groups[2].Value.Trim();

                // Multiline header support: look ahead if title is empty
                if (string.IsNullOrEmpty(title) && i + 1 < lines.Count)
                {
                    var nextLine = lines[i + 1].Trim();
                    if (!IsGoalOrTaskLine(nextLine) && !tocLeaderRegex.IsMatch(nextLine) && nextLine.Length > 0)
                    {
                        title = nextLine;
                        i++; // consume
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
                // Multiline header support: look ahead if title is empty
                if (string.IsNullOrEmpty(ogTitle) && i + 1 < lines.Count)
                {
                    var nextLine = lines[i + 1].Trim();
                    if (!IsGoalOrTaskLine(nextLine) && !tocLeaderRegex.IsMatch(nextLine) && nextLine.Length > 0)
                    {
                        ogTitle = nextLine;
                        i++; // consume
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
            if (isInTasksSection && currentOG != null)
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
                    currentOG.ProgramTasks.Add(new ProgramTaskDto
                    {
                        Label = $"{currentOG.Label}.{taskIndex}",
                        Number = taskIndex,
                        Description = cleanDesc
                    });
                }
            }
        }

        // Consolidate repeated goals from contents, summaries and main sections.
        foreach (var sg in strategicGoals)
        {
            sg.OperationalGoals = sg.OperationalGoals
                .GroupBy(og => og.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => {
                    var primary = g
                        .OrderBy(x => GoalTitleScore(x.Title))
                        .First();
                    var allTasks = g
                        .SelectMany(x => x.ProgramTasks)
                        .GroupBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
                        .Select(gt => gt.OrderByDescending(t => t.Description.Length).First())
                        .OrderBy(t => t.Number)
                        .ToList();
                    primary.ProgramTasks = allTasks;
                    return primary;
                })
                .Where(og => og.ProgramTasks.Count > 0)
                .ToList();
        }

        strategy.StrategicGoals = strategicGoals
            .GroupBy(sg => sg.Label, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var primary = group
                    .OrderBy(goal => GoalTitleScore(goal.Title))
                    .First();
                primary.OperationalGoals = group
                    .SelectMany(goal => goal.OperationalGoals)
                    .GroupBy(goal => goal.Label, StringComparer.OrdinalIgnoreCase)
                    .Select(operationalGroup =>
                    {
                        var operationalPrimary = operationalGroup
                            .OrderBy(goal => GoalTitleScore(goal.Title))
                            .First();
                        operationalPrimary.ProgramTasks = operationalGroup
                            .SelectMany(goal => goal.ProgramTasks)
                            .GroupBy(task => task.Label, StringComparer.OrdinalIgnoreCase)
                            .Select(taskGroup => taskGroup.OrderByDescending(task => task.Description.Length).First())
                            .OrderBy(task => task.Number)
                            .ToList();
                        return operationalPrimary;
                    })
                    .OrderBy(goal => goal.Number)
                    .ToList();
                return primary;
            })
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

        var strategicGoalRegex = new Regex(
            @"^\s*(?:Стратегічна ціль|Стратегічний напрямок|Стратегічний пріоритет|Пріоритетний напрям|Напрям|Пріоритет)\s+(\d+|[IVX]+)\b",
            RegexOptions.IgnoreCase);

        var firstGoalIndex = -1;
        var firstGoalLabel = "";
        for (var i = tocIndex + 1; i < lines.Count; i++)
        {
            var match = strategicGoalRegex.Match(lines[i]);
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
            var match = strategicGoalRegex.Match(lines[i]);
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
