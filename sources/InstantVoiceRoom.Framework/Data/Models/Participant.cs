using System;
using System.ComponentModel.DataAnnotations;
using InstantVoiceRoom.Framework.Data.Model;

namespace InstantVoiceRoom.Web.Model;

public class Participant
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ConnectionId { get; set; } // SignalR Connection ID

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; }

    public int RoomId { get; set; } // Foreign key to Room
    public Room Room { get; set; } // Navigation property

    public bool IsCameraOn { get; set; } = true; // Default to camera on
    public bool IsMicOn { get; set; } = true;    // Default to mic on
}