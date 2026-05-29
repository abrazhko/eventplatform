namespace EventPlatform.Models.Dto;
public class FilterRequest
{
    public string? Search { get; set; }

    public string? Category { get; set; }

    public string? DateFrom { get; set; }

    public string? DateTo { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    public bool? OnlyFree { get; set; }
}