using System.ComponentModel.DataAnnotations;

namespace SubspaceStats.Areas.League.Models.Franchise
{
    // For CRUD methods
    // GET /Franchise/Create
    // POST /Franchise/Create
    // GET /Franchise/Edit
    // POST /Franchise/Edit
    // GET /Franchise/Delete
    // POST /Franchise/Delete
    public class FranchiseModel : IFranchiseBreadcrumb
    {
        [Display(Name = "Franchise ID")]
        public required long Id { get; set; }

        [Display(Name = "Franchise Name")]
        [Required]
        [StringLength(64, MinimumLength = 1)]
        public required string Name { get; set; }
    }

    // For listing on GET /franchise
    public class FranchiseListItem : FranchiseModel
    {
        public required string[] Teams { get; set; }
    }
}
