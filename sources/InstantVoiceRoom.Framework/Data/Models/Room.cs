using System;
using System.ComponentModel.DataAnnotations;
using InstantVoiceRoom.Framework.Data.Models;
using InstantVoiceRoom.Web.Model;

namespace InstantVoiceRoom.Framework.Data.Model;

public class Room
{
    public int Id { get; set; }


    public Guid UserId { get; set; }

    public User User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } // Unique name for the room

    // Navigation property for participants in this room
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}