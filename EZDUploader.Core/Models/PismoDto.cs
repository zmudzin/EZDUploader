using EZDUploader.Core.Models;

public class PismoDto
{
    public required int ID { get; set; }
    public required string Nazwa { get; set; }
    public int? Rodzaj { get; set; }
    public required DateTime DataUtworzenia { get; set; }
    public required bool Zawieszone { get; set; }
    public required bool Zakonczone { get; set; }
    public DateTime? DataZakonczenia { get; set; }
    public DateTime? TerminPisma { get; set; }
    public required bool KoszulkaWrazliwa { get; set; }
    public required bool CzyZarchiwizowany { get; set; }
    public StanowiskoDto? TworcaStanowisko { get; set; }
}