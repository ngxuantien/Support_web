using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace NCKH.Models
{
    public class DatabaseHelper
    {
        private string connectionString = "server=localhost;database=device_management;user=root;password=;port=3306;";

		public DatabaseHelper()
		{
		}

		// Hàm mở kết nối
		private MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public DataTable GetDeviceCode(int id)
        {
            DataTable dt = new DataTable();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT * FROM `device` WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }


        public DataTable GetDeviceInfoByCode(string code)
        {
            string query = "SELECT id, name, status, id_room, device_code FROM device WHERE device_code = @code";

            using (MySqlConnection conn = GetConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }


        // lấy danh sách reports
        public DataTable getReport()
		{
            DataTable dt = new DataTable();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT 
                    r.id, 
                    d.name AS device_name, 
                    rm.name AS room_name, 
                    a.name AS area_name, 
                    r.description, 
                    r.time_repair, 
                    g.name AS name_group
                FROM report r 
                JOIN device d ON r.id_device = d.id 
                JOIN room rm ON d.id_room = rm.id 
                JOIN area a ON rm.id_area = a.id 
                JOIN `group` g ON r.id_group = g.id
                WHERE r.time_repair IS NOT NULL;
                ";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }
        // Hàm lấy danh sách Users
        public DataTable GetUsers()
        {
            DataTable dt = new DataTable();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM user";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }
        // hàm đăng nhập
        public bool Login(string username, string password)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();

                // Truy vấn kiểm tra cả username và password
                string query = "SELECT COUNT(*) FROM admin WHERE name = @username AND pass = @password";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        // đăng ký
        public bool Register(string username, string passwork)
		{
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();

                // Kiểm tra xem username đã tồn tại chưa
                string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@username", username);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        return false; // Tên đăng nhập đã tồn tại
                    }
                }

                // Mã hóa mật khẩu bằng BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(passwork, BCrypt.Net.BCrypt.GenerateSalt(12));

                // Chèn tài khoản mới vào database
                string insertQuery = "INSERT INTO users (username, password) VALUES (@username, @password)";
                using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@password", hashedPassword);

                    return insertCmd.ExecuteNonQuery() > 0; // Trả về true nếu đăng ký thành công
                }
            }
        }
        // Hàm thêm user
        public void AddUser(string username, string password)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string query = "INSERT INTO users (username, password) VALUES (@username, @password)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // Lấy danh sách báo cáo chưa phân công
        public DataTable GetUnassignedReports()
        {
            DataTable reports = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = @"WITH ranked_reports AS 
                            (
                                SELECT 
                                    r.id,
                                    d.name AS name_device,
                                    ro.name AS name_room,
                                    r.description,
                                    d.status,
                                    r.id_group,
                                    r.time_report,
                                    ROW_NUMBER() OVER (PARTITION BY d.name ORDER BY r.time_report DESC) AS rn
                                FROM report r
                                JOIN device d ON r.id_device = d.id
                                JOIN room ro ON ro.id = d.id_room
                                WHERE d.status IN ('Hỏng', 'Đang sửa')
                            )
                                SELECT 
                                    id, name_device, name_room, description, status, id_group, time_report
                                FROM ranked_reports
                                WHERE rn = 1;
                                ";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(reports);
                    }
                }
            }
            return reports;
        }

        public DataTable GetStatusInDevice()
        {
            DataTable reports = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = @"SELECT * FROM report r JOIN device d ON r.id_device = d.id WHERE d.status = 'Hỏng';
                                ";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(reports);
                    }
                }
            }
            return reports;
        }

        public int GetReportCount()
            {
                string query = @"SELECT COUNT(*) AS report_count FROM device d WHERE d.status = 'Hỏng';

                                ";
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
        // số thiết bị đang hỏng

        public int GetErrorDevice()
        {
            string query = @"SELECT COUNT(*) FROM `device` WHERE status='Hỏng';
                                ";
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        // số lượng thiết bị
        public int GetDeviceCount()
        {
            string query = @"SELECT COUNT(*) FROM `device`;"; 
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();

                        // Kiểm tra nếu result là null hoặc DBNull
                        if (result == null || result == DBNull.Value)
                        {
                            return 0;
                        }

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để dễ dàng theo dõi
                Console.WriteLine($"Lỗi khi lấy số lượng thiết bị: {ex.Message}");
                return -1; // Trả về -1 nếu có lỗi xảy ra
            }
        }

        // đã sửa tháng này
        public int GetCorrected()
        {
            string query = @"SELECT COUNT(*)
                            FROM report
                            WHERE 
                                time_repair IS NOT NULL
                                AND YEAR(time_repair) = YEAR(NOW())
                                AND MONTH(time_repair) = MONTH(NOW())
                            ";
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();

                        // Kiểm tra nếu result là null hoặc DBNull
                        if (result == null || result == DBNull.Value)
                        {
                            return 0;
                        }

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để dễ dàng theo dõi
                Console.WriteLine($"Lỗi khi lấy số lượng thiết bị: {ex.Message}");
                return -1; // Trả về -1 nếu có lỗi xảy ra
            }
        }

        // đã sửa tháng trước
        public int GetCorrectedBefore()
        {
            string query = @"SELECT COUNT(*)
                            FROM report
                            WHERE 
                                time_repair IS NOT NULL
                                AND YEAR(time_repair) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH)
                                AND MONTH(time_repair) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)
                            ";
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();

                        // Kiểm tra nếu result là null hoặc DBNull
                        if (result == null || result == DBNull.Value)
                        {
                            return 0;
                        }

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để dễ dàng theo dõi
                Console.WriteLine($"Lỗi khi lấy số lượng thiết bị: {ex.Message}");
                return -1; // Trả về -1 nếu có lỗi xảy ra
            }
        }


        // Lấy danh sách nhóm xử lý
        public DataTable GetUserGroups()
        {
            DataTable groups = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = "SELECT id, name FROM `group`";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(groups);
                    }
                }
            }
            return groups;
        }

        // Cập nhật trạng thái phân công báo cáo
        public bool AssignReportToGroup(int reportId, int groupId)
        {
            using (MySqlConnection con = GetConnection())
            {
                string query = "UPDATE report SET id_group = @groupId WHERE id = @reportId";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.Parameters.AddWithValue("@reportId", reportId);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public DataTable UpdateStatus()
        {
            DataTable dt = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = @"
                    UPDATE `device` d
                    JOIN (
                        SELECT id_device,
                               COUNT(*) AS total,
                               COUNT(time_repair) AS repaired,
                               COUNT(id_group) AS group_assigned
                        FROM `report`
                        GROUP BY id_device
                    ) r ON d.id = r.id_device
                    SET d.status = 
                        CASE 
                            WHEN r.group_assigned > r.repaired THEN 'đang sửa'
                            WHEN r.total = r.repaired THEN 'hoạt động'
                            WHEN r.total < r.repaired THEN 'hỏng'
                            ELSE d.status
                        END;
                    ";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public DataTable GetRooms()
        {
            DataTable dt = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = @"
                    SELECT r.id, a.name AS area_name, r.name AS room_name
                    FROM room r
                    JOIN area a ON r.id_area = a.id
                    ORDER BY a.name ASC, r.name ASC;
                    ";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public DataTable GetAreaRoom()
        {
            DataTable dt = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = @"SELECT a.name area_name, a.id area_id, r.name room_name, r.id room_id 
                               FROM area a JOIN room r 
                               WHERE a.id = 1 and r.id_area = 1;";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public DataTable GetAreas()
        {
            DataTable area = new DataTable();
            using (MySqlConnection con = GetConnection())
            {
                string query = "SELECT id, name FROM area";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(area);
                    }
                }
            }
            return area;
        }

        public void AddRoom(int id_area, string room_name)
        {
            using (MySqlConnection con = GetConnection())
            {
                string query = "INSERT INTO room (name, id_area) VALUES (@name, @id_area)";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@name", room_name);
                    cmd.Parameters.AddWithValue("@id_area", id_area);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public void AddArea(string name)
        {
            using (MySqlConnection con = GetConnection())
            {
                string query = "INSERT INTO area (name) VALUES (@name)";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Chỉnh sửa tên dãy phòng
        public void EditArea(int areaId, string areaName)
        {
            using (MySqlConnection conn = GetConnection())
            {
                string query = "UPDATE area SET name = @name WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", areaName);
                    cmd.Parameters.AddWithValue("@id", areaId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Xóa dãy phòng
        public void DeleteArea(int areaId)
        {
            using (MySqlConnection con = GetConnection())
            {
                string query = "DELETE FROM area WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", areaId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateRoom(int room_id, string room_name)
        {
            using (MySqlConnection con = GetConnection())
            {
                string query = "UPDATE room SET name = @name WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@name", room_name);
                    cmd.Parameters.AddWithValue("@id", room_id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteRoom(int room_id)
        {
            using (MySqlConnection con = GetConnection())
            {
                string query = "DELETE FROM room WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", room_id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        //public DataTable GetDevice()
        //{
        //    DataTable device = new DataTable();
        //    using (MySqlConnection conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = "SELECT d.id AS device_id, d.name AS device_name,  d.status,  r.id AS room_id,  r.name AS room_name, a.id AS area_id, a.name AS area_name FROM device d JOIN room r ON d.id_room = r.id JOIN area a ON r.id_area = a.id;";
        //        using (MySqlCommand cmd = new MySqlCommand(query, conn))
        //        {
        //            using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
        //            {
        //                adapter.Fill(device);
        //            }
        //        }
        //    }
        //    return device;
        //}


        // Lấy danh sách thiết bị
        public DataTable GetDevices()
        {
            string query = @"SELECT d.id AS device_id, d.name AS device_name, d.status, 
                                r.id AS room_id, r.name AS room_name, a.id AS area_id, a.name AS area_name
                         FROM device d 
                         JOIN room r ON d.id_room = r.id 
                         JOIN area a ON r.id_area = a.id
                         ORDER BY d.id ASC";

            using (MySqlConnection conn = GetConnection())
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // Lấy danh sách phòng theo dãy đã chọn
        public DataTable GetRoomsByArea(int areaId)
        {
            string query = "SELECT id, name FROM room WHERE id_area = @areaId ORDER BY name ASC";

            using (MySqlConnection conn = GetConnection())
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@areaId", areaId);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // Thêm thiết bị mới
        public void AddDevice(string name, string status, int roomId)
        {
            string query = "INSERT INTO device (name, status, id_room) VALUES (@name, @status, @roomId)";

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@roomId", roomId);
                cmd.ExecuteNonQuery();
            }
        }

        // Xóa thiết bị
        public void DeleteDevice(int id)
        {
            string query = "DELETE FROM device WHERE id = @id";

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}