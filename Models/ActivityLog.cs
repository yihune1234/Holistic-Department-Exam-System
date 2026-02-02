using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("action")]
        [StringLength(255)]
        public string Action { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Column("ip_address")]
        [StringLength(50)]
        public string IpAddress { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string DeviceInfo { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
