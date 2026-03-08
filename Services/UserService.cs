using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class UserService
{
    public User? Login(string username, string password)
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

    private void UpdateLastLoginTime(int userId)
    {
        var sql = "UPDATE DM_User SET LastLoginTime = GETDATE() WHERE UserID = @UserID";
        DbHelper.ExecuteNonQuery(sql, new SqlParameter("@UserID", userId));
    }

    public List<User> GetAllUsers()
    {
        var sql = "SELECT * FROM DM_User ORDER BY UserID";
        return DbHelper.ExecuteQuery(sql, MapToUser);
    }

    public User? GetUserById(int userId)
    {
        var sql = "SELECT * FROM DM_User WHERE UserID = @UserID";
        var users = DbHelper.ExecuteQuery(sql, MapToUser, new SqlParameter("@UserID", userId));
        return users.FirstOrDefault();
    }

    public int CreateUser(User user)
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

    public bool UpdateUser(User user)
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

    public bool UpdatePassword(int userId, string newPassword)
    {
        var sql = "UPDATE DM_User SET Password = @Password WHERE UserID = @UserID";
        return DbHelper.ExecuteNonQuery(sql,
            new SqlParameter("@UserID", userId),
            new SqlParameter("@Password", newPassword)) > 0;
    }

    public bool DeleteUser(int userId)
    {
        var sql = "DELETE FROM DM_User WHERE UserID = @UserID";
        return DbHelper.ExecuteNonQuery(sql, new SqlParameter("@UserID", userId)) > 0;
    }

    public bool IsUsernameExists(string username, int? excludeUserId = null)
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
