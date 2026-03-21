using System.ComponentModel.DataAnnotations;

namespace FoodSafetyTracker.Models;

public enum RiskRating { Low, Medium, High }
public enum InspectionOutcome { Pass, Fail }
public enum FollowUpStatus { Open, Closed }

public class Premises
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Town { get; set; } = string.Empty;

    public RiskRating RiskRating { get; set; }

    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
}

public class Inspection
{
    public int Id { get; set; }

    public int PremisesId { get; set; }
    public Premises Premises { get; set; } = null!;

    [Required]
    public DateTime InspectionDate { get; set; }

    [Range(0, 100)]
    public int Score { get; set; }

    public InspectionOutcome Outcome { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}

public class FollowUp
{
    public int Id { get; set; }

    public int InspectionId { get; set; }
    public Inspection Inspection { get; set; } = null!;

    [Required]
    public DateTime DueDate { get; set; }

    public FollowUpStatus Status { get; set; }

    public DateTime? ClosedDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
