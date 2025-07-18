using InstantVoiceRoom.Framework.Data;
using InstantVoiceRoom.Framework.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantVoiceRoom.Framework.Services;

public class UserService(ApplicationDbContext applicationDbContext, ILogger<UserService> logger) : IUserService
{
  private readonly ApplicationDbContext _applicationDbContext = applicationDbContext;
  private readonly ILogger<UserService> _logger = logger;

  public async Task<bool> AddUser(string userName, string password)
  {
    if (await _applicationDbContext.Users.AnyAsync(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
      return false;

    var protectedPwd = PasswordHasher.Hash(password);
    _applicationDbContext.Users.Add(new User { Id = Guid.NewGuid(), UserName = userName, Password = protectedPwd });
    await _applicationDbContext.SaveChangesAsync();
    return true;
  }

  public async Task<bool> DeleteUser(string userName)
  {
    var found = await _applicationDbContext.Users.SingleOrDefaultAsync(dd => dd.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
    if (found == null)
      return false;
    _applicationDbContext.Users.Remove(found);
    await _applicationDbContext.SaveChangesAsync();
    return true;
  }

  public async Task<bool> ValidateCredentials(string userName, string password)
  {
    var record = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
    if (record == null) return false;

    try
    {
      return PasswordHasher.Verify(password, record.Password);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, ex.Message);
      return false;
    }
  }
}
