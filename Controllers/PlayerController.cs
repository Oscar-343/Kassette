using Kassette.Services;
using Microsoft.AspNetCore.Mvc;
namespace Kassette.Controllers;

public record PlayRequest(int[] Ids, int StartId);

[ApiController, Route("api/player")]
public class PlayerController(PlayerRegistry registry) : ControllerBase
{
    private PlayerService P => registry.For(HttpContext); // el reproductor de ESTE navegador

    [HttpGet("state")] public PlayerState State() => P.Snapshot();
    [HttpPost("play")] public PlayerState Play(PlayRequest r) => P.Play(r.Ids, r.StartId);
    [HttpPost("toggle")] public PlayerState Toggle() => P.Toggle();
    [HttpPost("next")] public PlayerState Next([FromQuery] bool auto = false) => P.Next(auto);
    [HttpPost("previous")] public PlayerState Previous() => P.Previous();
    [HttpPost("enqueue/{id:int}")] public PlayerState Enqueue(int id) => P.Enqueue(id);
    [HttpPost("shuffle")] public PlayerState Shuffle() => P.ToggleShuffle();
    [HttpPost("repeat")] public PlayerState Repeat() => P.CycleRepeat();
}
