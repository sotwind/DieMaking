using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class UserService
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
        try
        {
            var sql = "SELECT * FROM DM_User ORDER BY UserID";
            return DbHelper.ExecuteQuery(sql, MapToUser);
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取所有用户");
            return new List<User>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取所有用户");
            return new List<User>();
        }
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public User? GetUserById(int userId)
    {
        try
        {
            var sql = "SELECT * FROM DM_User WHERE UserID = @UserID";
            var users = DbHelper.ExecuteQuery(sql, MapToUser, new SqlParameter("@UserID", userId));
            return users.FirstOrDefault();
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"获取用户(ID:{userId})");
            return null;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"获取用户(ID:{userId})");
            return null;
        }
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

            var result = DbHelper.ExecuteScalar(sql,
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
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "创建用户");
            return 0;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "创建用户");
            return 0;
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public bool UpdateUser(User user)
    {
        try
        {
            var sql = @"UPDATE DM_User SET 
                         RealName = @RealName,
                         Permissions = @Permissions, 
                         Workstation = @Workstation,
                         IsActive = @IsActive
                         WHERE UserID = @UserID";

            return DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", user.UserID),
                new SqlParameter("@RealName", user.RealName),
                new SqlParameter("@Permissions", user.Permissions),
                new SqlParameter("@Workstation", user.Workstation),
                new SqlParameter("@IsActive", user.IsActive)) > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"更新用户(ID:{user.UserID})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"更新用户(ID:{user.UserID})");
            return false;
        }
    }

    /// <summary>
    /// 更新用户密码
    /// </summary>
    public bool UpdatePassword(int userId, string newPassword)
    {
        try
        {
            var sql = "UPDATE DM_User SET Password = @Password WHERE UserID = @UserID";
            return DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", userId),
                new SqlParameter("@Password", newPassword)) > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"更新密码(UserID:{userId})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"更新密码(UserID:{userId})");
            return false;
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public bool DeleteUser(int userId)
    {
        try
        {
            var sql = "DELETE FROM DM_User WHERE UserID = @UserID";
            return DbHelper.ExecuteNonQuery(sql, new SqlParameter("@UserID", userId)) > 0;
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ExceptionHelper.HandleException(new BusinessException("该用户有关联数据，无法删除。"), "删除用户");
            return false;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"删除用户(ID:{userId})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"删除用户(ID:{userId})");
            return false;
        }
    }

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    public bool IsUsernameExists(string username, int? excludeUserId = null)
    {
        try
        {
            var sql = excludeUserId.HasValue
                ? "SELECT COUNT(*) FROM DM_User WHERE Username = @Username AND UserID != @UserID"
                : "SELECT COUNT(*) FROM DM_User WHERE Username = @Username";

            var parameters = new List<SqlParameter> { new SqlParameter("@Username", username) };
            if (excludeUserId.HasValue)
                parameters.Add(new SqlParameter("@UserID", excludeUserId.Value));

            var result = DbHelper.ExecuteScalar(sql, parameters.ToArray());
            return Convert.ToInt32(result) > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "检查用户名是否存在");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "检查用户名是否存在");
            return false;
        }
    }

    /// <summary>
    /// 将数据读取器映射为用户对象
    /// </summary>
    private User MapToUser(SqlDataReader reader)
    {
        return new User
        {
            UserID = Convert.ToInt32(reader["UserID"]),
            Username = reader["Username"].ToString() ?? "",
            Password = reader["Password"].ToString() ?? "",
            RealName = reader["RealName"].ToString() ?? "",
            Permissions = reader["Permissions"].ToString() ?? "",
            Workstation = reader["Workstation"].ToString() ?? "",
            IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
            CreateTime = reader["CreateTime"] != DBNull.Value ? Convert.ToDateTime(reader["CreateTime"]) : DateTime.Now,
            LastLoginTime = reader["LastLoginTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastLoginTime"]) : null
        };
    }
}
