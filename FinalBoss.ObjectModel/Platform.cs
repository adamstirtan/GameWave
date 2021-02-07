﻿using System.ComponentModel.DataAnnotations;

namespace FinalBoss.ObjectModel
{
    public class Platform : BaseEntity
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int Generation { get; set; }
    }
}