using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class UserService : BaseService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    public User? Login(string username, string password)
    {
        try
        {
            var sql = @"SELECT UserID, Username, Password, RealName, Permissions, Workstation, IsActive, CreateTime, LastLoginTime 
                         FROM DM_User WHERE Username = @Username AND IsActive = 1";

            using var connection = DbHelper.CreateConnection();
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var dbPassword = reader["Password"].ToString() ?? "";
                if (password == dbPassword)
                {
                    var user = MapToUser(reader);
                    // 更新最后登录时间
                    UpdateLastLoginTime(user.UserID);
                    return user;
                }
            }

            return null;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "用户登录");
            return null;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "用户登录");
            return null;
        }
    }

    /// <summary>
    /// 更新用户最后登录时间
    /// </summary>
    private void UpdateLastLoginTime(int userId)
    {
        try
        {
            var sql = "UPDATE DM_User SET LastLoginTime = GETDATE() WHERE UserID = @UserID";
            DbHelper.ExecuteNonQuery(sql, new SqlParameter("@UserID", userId));
        }
        catch (Exception ex)
        {
            // 登录时间更新失败不影响登录流程，仅记录日志
            ExceptionHelper.HandleExceptionSilent(ex, "更新最后登录时间");
        }
    }

    /// <summary>
    /// 获取所有用户
    /// </summary>
    public List<User> GetAllUsers()
    {
        return GetAll("DM_User", "UserID", MapToUser);
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public User? GetUserById(int userId)
    {
        return GetById("DM_User", "UserID", userId, MapToUser);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public int CreateUser(User user)
    {
        try
        {
            var sql = @"INSERT INTO DM_User (Username, Password, RealName, Permissions, Workstation, IsActive, CreateTime) 
                         VALUES (@Username, @Password, @RealName, @Permissions, @Workstation, @IsActive, GETDATE());
                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var result = ExecuteScalarSafe(sql, "创建用户",
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@Password", user.Password),
                new SqlParameter("@RealName", user.RealName),
                new SqlParameter("@Permissions", user.Permissions),
                new SqlParameter("@Workstation", user.Workstation),
                new SqlParameter("@IsActive", user.IsActive));

            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            ExceptionHelper.HandleException(new BusinessException("用户名已存在，请使用其他用户名。"), "创建用户");
            return 0;
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public bool UpdateUser(User user)
    {
        var sql = @"UPDATE DM_User SET 
                     Username = @Username,
                     RealName = @RealName,
                     Permissions = @Permissions, 
                     Workstation = @Workstation,
                     IsActive = @IsActive
                     WHERE UserID = @UserID";

        return ExecuteNonQuerySafe(sql, $"更新用户(ID:{user.UserID})",
            new SqlParameter("@UserID", user.UserID),
            new SqlParameter("@Username", user.Username),
            new SqlParameter("@RealName", user.RealName),
            new SqlParameter("@Permissions", user.Permissions),
            new SqlParameter("@Workstation", user.Workstation),
            new SqlParameter("@IsActive", user.IsActive)) > 0;
    }

    /// <summary>
    /// 更新用户密码
    /// </summary>
    public bool UpdatePassword(int userId, string newPassword)
    {
        var sql = "UPDATE DM_User SET Password = @Password WHERE UserID = @UserID";
        return ExecuteNonQuerySafe(sql, $"更新密码(UserID:{userId})",
            new SqlParameter("@UserID", userId),
            new SqlParameter("@Password", newPassword)) > 0;
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public bool DeleteUser(int userId)
    {
        return Delete("DM_User", "UserID", userId, "用户");
    }

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    public bool IsUsernameExists(string username, int? excludeUserId = null)
    {
        return Exists("DM_User", "Username", username, excludeUserId, "UserID");
    }

    /// <summary>
    /// 将数据读取器映射为用户对象
    /// </summary>
    private User MapToUser(SqlDataReader reader)
    {
        return new User
        {
            UserID = ConvertHelper.ToInt(reader["UserID"]),
            Username = ConvertHelper.ToString(reader["Username"]),
            Password = ConvertHelper.ToString(reader["Password"]),
            RealName = ConvertHelper.ToString(reader["RealName"]),
            Permissions = ConvertHelper.ToString(reader["Permissions"]),
            Workstation = ConvertHelper.ToString(reader["Workstation"]),
            IsActive = ConvertHelper.ToBool(reader["IsActive"]),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now),
            LastLoginTime = ConvertHelper.ToNullableDateTime(reader["LastLoginTime"])
        };
    }
}
