using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncSyntax.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        // تحديد المستخدم الذي سيستقبل الإشعار
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public int? PostId { get; set; }

        // تحديد المنشور المرتبط بالإشعار
        [ForeignKey("PostId")]
        public Post? Post { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
