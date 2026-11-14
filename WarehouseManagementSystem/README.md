# WAREHOUSE MANAGEMENT SYSTEM (WMS)
## HỆ THỐNG QUẢN LÝ KHO LINH KIỆN ĐIỆN TỬ DÀNH CHO DOANH NGHIỆP SẢN XUẤT

> **Hệ thống Phần mềm Quản trị Sản xuất & Kho Vận (WMS / MES Module)**  
> **Giải pháp:** Quản lý kho nguyên vật liệu, linh kiện điện tử chuyên sâu cho doanh nghiệp sản xuất.

---

## 1. GIỚI THIỆU DỰ ÁN
**Warehouse Management System (WMS)** là hệ thống quản trị kho vật tư, linh kiện điện tử chuyên nghiệp được xây dựng trên nền tảng **ASP.NET Core MVC** và **Microsoft SQL Server**.

Hệ thống giải quyết trọn vẹn bài toán quản lý kho trong dây chuyền sản xuất bo mạch điện tử:
- Quản lý kho đa vị trí (**Multi-location Inventory**): Một linh kiện có thể lưu tại nhiều ngăn kệ khác nhau.
- Quy trình Nhập kho (**Stock In**) và Xuất kho (**Stock Out**) với cơ chế kiểm soát toàn vẹn dữ liệu nghiêm ngặt (**ACID Transactions** & **Concurrency Control**).
- Không bao giờ cho phép xuất vượt tồn kho hoặc để tồn kho bị âm.
- Sổ cái lịch sử biến động kho (**Audit Trail**) bất biến.
- Cảnh báo định mức tồn kho tối thiểu và thống kê sản lượng trực quan bằng biểu đồ.

---

## 2. CÔNG NGHỆ SỬ DỤNG

| Thành phần | Công nghệ |
| :--- | :--- |
| **Backend** | C# (.NET 10.0 LTS), ASP.NET Core MVC |
| **Truy cập CSDL** | Entity Framework Core 9.0.2 (Code-First & Migrations), LINQ |
| **Cơ sở dữ liệu** | Microsoft SQL Server (MSSQLSERVER) |
| **Xác thực & Phân quyền** | ASP.NET Core Identity (Role-Based Access Control: Admin / Staff) |
| **Kiến trúc** | MVC + Service Layer + Dependency Injection + ViewModels |
| **Frontend** | Razor Views, HTML5, CSS3, Bootstrap 5, Bootstrap Icons, JavaScript, Chart.js |
| **Kiểm thử (Testing)** | xUnit, Moq, Microsoft.EntityFrameworkCore.Sqlite In-Memory |

---

## 3. CÁC MODULE CHỨC NĂNG CỐT LÕI

1. **Authentication & Authorization:** Đăng nhập, đăng xuất bảo mật, phân quyền 2 cấp độ:
   - `Admin`: Toàn quyền quản trị danh mục, vị trí kho, nhà cung cấp, linh kiện, phê duyệt phiếu nhập/xuất, quản lý nhân viên.
   - `Staff`: Lập phiếu nhập/xuất ở trạng thái Nháp, theo dõi số lượng tồn kho theo vị trí kệ, xem lịch sử giao dịch.
2. **Dashboard:** 5 thẻ KPI thống kê tổng quan, biểu đồ so sánh sản lượng nhập/xuất 6 tháng gần nhất bằng Chart.js, bảng 10 giao dịch gần nhất, danh sách linh kiện dưới mức an toàn.
3. **Quản lý Linh kiện (Products):** Mã linh kiện duy nhất (`ProductCode`), phân loại theo danh mục và nhà cung cấp, đơn vị tính, mức tồn tối thiểu (`MinimumStock`), xem chi tiết tồn kho theo vị trí và lịch sử giao dịch riêng.
4. **Quản lý Danh mục & Nhà cung cấp:** Quản lý đối tác bán dẫn và danh mục; có ràng buộc toàn vẹn không cho xóa nếu đang có sản phẩm liên kết.
5. **Quản lý Vị trí kho (Warehouse Locations):** Quản lý mã vị trí kệ duy nhất, theo dõi các mặt hàng đang lưu trữ tại từng ngăn kệ.
6. **Nhập kho (Stock In):** Tạo phiếu nhập với nhiều dòng linh kiện; mã phiếu tự sinh `PN-yyyyMMdd-xxxx`; duyệt phiếu bằng Database Transaction tăng tồn kho và ghi sổ cái giao dịch.
7. **Xuất kho (Stock Out):** Kiểm tra số lượng tồn khả dụng thời gian thực bằng AJAX; kiểm tra concurrency trong Database Transaction; không cho phép xuất vượt tồn kho hoặc âm kho.
8. **Quản lý Tồn kho (Inventories):** Theo dõi tồn kho đa vị trí, phân loại trạng thái màu sắc (*Bình thường*, *Sắp hết hàng*, *Hết hàng*), lọc danh mục, tìm kiếm, phân trang.
9. **Sổ cái Lịch sử Biến động (Stock Transactions):** Ghi nhận đầy đủ dấu vết biến động (Audit Trail), người thực hiện, thời gian, chứng từ tham chiếu.
10. **Báo cáo & Xuất dữ liệu:** Báo cáo tồn kho, Top 10 linh kiện xuất nhiều nhất, xuất file CSV định dạng **UTF-8 with BOM** mở bằng Excel không bị lỗi font tiếng Việt.

---

## 4. HƯỚNG DẪN CÀI ĐẶT & CHẠY ỨNG DỤNG

### 4.1. Yêu cầu môi trường
- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (hoặc tương thích với .NET LTS).
- [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (SQL Server 2019/2022/2025 hoặc SQL Express / LocalDB).

### 4.2. Cấu hình Chuỗi kết nối Database
Mở file `WarehouseManagementSystem/appsettings.json`, kiểm tra chuỗi kết nối:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=WarehouseDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
}
```
*(Nếu bạn sử dụng SQL Express, đổi `Server=localhost` thành `Server=localhost\\SQLEXPRESS`)*.

### 4.3. Chạy Migration tạo Cơ sở dữ liệu
Mở Terminal tại thư mục `WarehouseManagementSystem`:
```bash
dotnet ef database update
```
*(Lệnh này sẽ tự động tạo cơ sở dữ liệu `WarehouseDb` và toàn bộ các bảng, chỉ mục Unique Index, khóa ngoại và Check Constraint)*.

### 4.4. Chạy Ứng dụng
```bash
dotnet run
```
Mở trình duyệt web và truy cập: `https://localhost:5001` hoặc `http://localhost:5000`.

Hệ thống sẽ **tự động seed dữ liệu mẫu** (5 danh mục, 5 nhà cung cấp, 20 linh kiện điện tử, 6 vị trí kho, các phiếu nhập và xuất đã xác nhận khớp 100% tồn kho và lịch sử giao dịch).

---

## 5. TÀI KHOẢN DEMO KIỂM THỬ

Trên màn hình Đăng nhập đã có sẵn 2 nút bấm tự động điền tài khoản demo:

| Tài khoản | Email đăng nhập | Mật khẩu mặc định | Vai trò (Role) | Thẩm quyền |
| :--- | :--- | :--- | :--- | :--- |
| **Quản trị viên** | `admin@wms.com` | `Admin@123456` | **Admin** | Toàn quyền duyệt phiếu, quản lý danh mục, người dùng |
| **Nhân viên kho** | `staff@wms.com` | `Staff@123456` | **Staff** | Lập phiếu nháp, xem tồn kho, xem lịch sử giao dịch |

---

## 6. HƯỚNG DẪN CHẠY KIỂM THỬ (UNIT TEST)
Dự án có bộ kiểm thử tự động xUnit kiểm tra 100% các quy tắc nghiệp vụ quan trọng:
```bash
dotnet test ../WarehouseManagementSystem.Tests/WarehouseManagementSystem.Tests.csproj
```
**Kết quả kiểm thử:**
- `Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10`

---

## 7. TÀI LIỆU DỰ ÁN KÈM THEO
- [Thiết kế Cơ sở dữ liệu & Sơ đồ ERD](docs/DATABASE.md)
- [Quy trình Nghiệp vụ Kho](docs/BUSINESS_FLOW.md)
- [Cẩm nang Trả lời Phỏng vấn IT MES](docs/INTERVIEW_GUIDE.md)
- [Báo cáo Tổng kết Dự án](docs/PROJECT_SUMMARY.md)
