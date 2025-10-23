//using Application_Layer.Services;
//using Microsoft.AspNetCore.Mvc;

//[ApiController]
//[Route("api/sync")]
//public class SyncController : ControllerBase
//{
//    private readonly AccountingSyncManager _syncManager;

//    public SyncController(AccountingSyncManager syncManager)
//    {
//        _syncManager = syncManager;
//    }

//    [HttpPost("xero")]
//    public async Task<IActionResult> SyncXero()
//    {
//        await _syncManager.SyncFromXeroAsync();
//        return Ok("Xero sync completed");
//    }
//}
