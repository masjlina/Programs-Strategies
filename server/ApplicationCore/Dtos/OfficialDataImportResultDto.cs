namespace ApplicationCore.Dtos;

public class OfficialDataImportResultDto
{
    public int RegionsCreated { get; set; }
    public int DistrictsCreated { get; set; }
    public int CommunitiesCreated { get; set; }
    public int SettlementsCreated { get; set; }
    public int RowsProcessed { get; set; }
    public int RowsSkipped { get; set; }
}
