using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class Role
    {
        [Key]
        // Roles are often static seeds so we might NOT want identity here, but to be consistent with others and if we expect dynamic roles later:
        // However, usually Roles like Admin, Student, Coordinator are seeded with explicit IDs 1, 2, 3.
        // If we add Identity, seeding with explicit IDs requires IDENTITY_INSERT ON.
        // But for safety against the error generally, let's add it. But be careful about seeding.
        // Actually, looking at migration, Roles are seeded with 1, 2, 3.
        // If we add Identity, EF Core migration usually handles the implementation details (SET IDENTITY_INSERT ON).
        // Let's add it to be safe.
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [Column("role_name")]
        [StringLength(50)]
        public string RoleName { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
