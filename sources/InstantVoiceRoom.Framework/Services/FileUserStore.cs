using System.Text.Json;
using InstantVoiceRoom.Framework.Models;
using Microsoft.Extensions.Logging;

namespace InstantVoiceRoom.Framework.Services;

public class FileUserStore
{
  private readonly ILogger<FileUserStore> logger;
  private readonly string _filePath;
  private readonly object _lock = new();

  public FileUserStore(ILogger<FileUserStore> logger, string filePath)
  {
    this.logger = logger;
    _filePath = filePath;
    logger.LogInformation($"Using db file {filePath}");
    if (!File.Exists(_filePath))
      File.WriteAllText(_filePath, "[]");
  }

  public bool AddUser(string userName, string password)
  {
    lock (_lock)
    {
      var users = LoadAll();
      if (users.Any(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
        return false;

      var protectedPwd = PasswordHasher.Hash(password);
      users.Add(new UserRecord { UserName = userName, PasswordProtected = protectedPwd });
      SaveAll(users);
      return true;
    }
  }

  public bool DeleteUser(string userName)
  {
    lock (_lock)
    {
      var users = LoadAll();
      var removed = users.RemoveAll(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
      if (removed == 0) return false;
      SaveAll(users);
      return true;
    }
  }

  public bool ValidateCredentials(string userName, string password)
  {
    var users = LoadAll();
    var record = users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
    if (record == null) return false;

    try
    {
      return PasswordHasher.Verify(password, record.PasswordProtected);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, ex.Message);
      return false;
    }
  }

  private List<UserRecord> LoadAll()
  {
    var json = File.ReadAllText(_filePath);
    return JsonSerializer.Deserialize<List<UserRecord>>(json)
           ?? new List<UserRecord>();
  }

  private void SaveAll(List<UserRecord> users)
  {
    var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(_filePath, json);
  }
}
