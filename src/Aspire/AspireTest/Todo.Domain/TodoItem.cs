﻿using System.ComponentModel.DataAnnotations;

namespace Todo.Domain.Common
{
    public class TodoItem : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        //[ForeignKey("ApplicationUser")]
        //[Column(TypeName = "Id")]
        //public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
