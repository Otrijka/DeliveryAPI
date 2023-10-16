﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryApi.Models;

public class Rating
{
    [Range(0.0, 10.0, ErrorMessage = "Value must be between 0 and 10")]
    public double Value { get; set; }
    public Guid UserId { get; set; }
    public Guid DishId { get; set; }

    [ForeignKey("UserId")] 
    private UserDTO User { get; set; }

    [ForeignKey("DishId")]
    private DishDTO Dish { get; set; }
}