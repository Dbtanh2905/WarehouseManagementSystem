# TỔNG KẾT DỰ ÁN WMS - HỆ THỐNG QUẢN LÝ KHO LINH KIỆN ĐIỆN TỬ
*(Warehouse Management System for Electronics Manufacturing)*

---

## 1. THÔNG TIN CHUNG
- **Tên dự án:** Warehouse Management System (WMS) - Quản lý Kho Linh kiện Điện tử
- **Định hướng:** Xây dựng phần mềm quản lý kho chuyên nghiệp, áp dụng kiến thức ASP.NET Core MVC, SQL Server, Entity Framework Core và Unit Testing phục vụ doanh nghiệp sản xuất và hệ thống IT MES.

---

## 2. CÔNG NGHỆ THỰC SỰ ĐÃ SỬ DỤNG
- **Ngôn ngữ lập trình:** C# (.NET 10.0 LTS)
- **Nền tảng Web:** ASP.NET Core MVC
- **Truy cập dữ liệu (ORM):** Entity Framework Core 9.0.2 Code-First & Migrations
- **Cơ sở dữ liệu:** Microsoft SQL Server (MSSQLSERVER 17.00.1000)
- **Bảo mật & Phân quyền:** ASP.NET Core Identity (Role-Based Access Control: Admin & Staff)
- **Kiến trúc phần mềm:** Model-View-Controller (MVC) + Service Layer + Dependency Injection
- **Giao diện:** HTML5, CSS3 hiện đại, Bootstrap 5, Bootstrap Icons, JavaScript, Chart.js
- **Kiểm thử tự động:** xUnit, Moq, Microsoft.EntityFrameworkCore.Sqlite In-Memory (10/10 test case đạt kết quả Pass)

---

## 3. CÁC CHỨC NĂNG ĐÃ HOÀN THÀNH 100%

### Module 1: Authentication & Authorization (Xác thực & Phân quyền)
- Đăng nhập, đăng xuất bảo mật với Cookie Authentication, mật khẩu băm chuẩn ASP.NET Core Identity.
- Phân quyền theo 2 vai trò:
  + **Admin:** Toàn quyền quản trị danh mục, vị trí kho, nhà cung cấp, linh kiện, phê duyệt phiếu nhập/xuất, quản lý tài khoản nhân viên.
  + **Staff:** Lập phiếu nhập/xuất ở trạng thái Nháp, theo dõi số lượng tồn kho theo vị trí, xem sổ cái giao dịch.
- Giao diện có sẵn nút điền nhanh tài khoản demo phục vụ kiểm thử và trình diễn chức năng.

### Module 2: Dashboard (Bảng điều khiển tổng quan)
- 5 thẻ chỉ số đo lường KPI: Tổng loại linh kiện, Tổng số lượng tồn, Số phiếu nhập trong tháng, Số phiếu xuất trong tháng, Số mặt hàng dưới định mức an toàn.
- Biểu đồ Bar Chart so sánh sản lượng nhập và xuất 6 tháng gần nhất bằng Chart.js.
- Bảng danh sách 10 giao dịch nhập/xuất gần nhất.
- Hộp cảnh báo các linh kiện sắp hết hàng / hết hàng cần bổ sung gấp.

### Module 3: Product Management (Quản lý linh kiện điện tử)
- Quản lý mã linh kiện duy nhất (`ProductCode`), tên, danh mục, nhà cung cấp, đơn vị tính, mức tồn an toàn tối thiểu (`MinimumStock`), ghi chú thông số kỹ thuật.
- Tìm kiếm theo mã và tên, lọc theo danh mục, phân trang.
- Bật/tắt trạng thái sử dụng (`IsActive`).
- Màn hình xem chi tiết linh kiện hiển thị phân bổ tồn kho tại các kệ và lịch sử giao dịch riêng của sản phẩm.
- Cơ chế bảo vệ toàn vẹn: Không cho phép xóa sản phẩm đã có lịch sử nhập/xuất kho.

### Module 4: Category & Supplier Management (Danh mục & Nhà cung cấp)
- Quản lý 5 danh mục vật tư sản xuất điện tử.
- Quản lý 5 nhà cung cấp linh kiện bán dẫn (Intel, Samsung, Foxconn, Texas Instruments, Murata).
- Ràng buộc toàn vẹn dữ liệu: Không cho phép xóa danh mục hoặc nhà cung cấp đang có sản phẩm hoặc phiếu nhập liên kết.

### Module 5: Warehouse Location (Vị trí lưu trữ trong kho)
- Quản lý các ngăn kệ trong kho: Mã kệ (`LocationCode` duy nhất), Tên kệ, Ghi chú đặc thù (phòng sạch, ESD, tải trọng).
- Quản lý tồn kho theo mô hình đa vị trí: Một linh kiện có thể lưu tại nhiều kệ khác nhau.

### Module 6: Stock In (Nhập kho từ nhà cung cấp)
- Tạo phiếu nhập kho với dynamic items (chọn linh kiện, vị trí lưu, số lượng $> 0$).
- Mã phiếu tự sinh dạng `PN-yyyyMMdd-xxxx`.
- Phiếu tạo mới ở trạng thái `Draft` (Bản nháp), cho phép sửa hoặc hủy.
- **Xác nhận nhập kho (`Confirm`):** Chỉ Admin được phép duyệt; xử lý trong một `IDbContextTransaction` cập nhật đồng thời trạng thái phiếu, tăng số lượng trong bảng `Inventories` và ghi sổ cái `StockTransactions`.

### Module 7: Stock Out (Xuất kho cấp phát sản xuất)
- Tạo phiếu xuất kho với dynamic items.
- Mã phiếu tự sinh dạng `PX-yyyyMMdd-xxxx`.
- **Kiểm tra tồn kho theo thời gian thực:** Tích hợp AJAX gọi API kiểm tra số lượng tồn khả dụng tại vị trí được chọn, chặn không cho nhập số lượng vượt tồn.
- **Xác nhận xuất kho (`Confirm`):** Chạy trong Database Transaction với cơ chế khóa kiểm tra concurrency, trừ số lượng tồn kho, đảm bảo không bao giờ âm kho, tạo sổ cái giao dịch; Rollback 100% nếu có lỗi.

### Module 8: Inventory Management (Theo dõi tồn kho đa vị trí)
- Bảng tổng hợp tồn kho: Mã, tên, danh mục, các kệ đang lưu trữ kèm số lượng cụ thể tại từng kệ, định mức tối thiểu.
- Phân loại trạng thái màu sắc chuẩn Enterprise: *Bình thường*, *Sắp hết hàng* ($\le$ MinimumStock), *Hết hàng* ($= 0$).
- Lọc theo từ khóa, danh mục, trạng thái tồn kho, phân trang.

### Module 9: Stock Transaction History (Sổ cái lịch sử biến động kho)
- Ghi nhận đầy đủ: Thời gian, mã chứng từ tham chiếu (PN/PX), loại giao dịch (Nhập/Xuất), linh kiện, vị trí kệ, số lượng biến động, người thực hiện, ghi chú.
- Không cho phép chỉnh sửa hoặc xóa lịch sử kế toán kho.

### Module 10: Reports & Data Export (Báo cáo & Xuất dữ liệu)
- Báo cáo tồn kho hiện tại.
- Báo cáo Top 10 linh kiện có số lượng xuất dùng nhiều nhất theo khoảng thời gian.
- Tính năng xuất dữ liệu sang định dạng **CSV chuẩn UTF-8 có kèm BOM (`EF BB BF`)**, mở bằng Microsoft Excel hiển thị tiếng Việt hoàn hảo không bị vỡ font.

---

## 4. KẾT QUẢ KIỂM THỬ (TEST RESULTS)
Bộ kiểm thử tự động xUnit trong `WarehouseManagementSystem.Tests` đạt **100% Pass (10/10 tests)**:
1. `Test1_StockIn_IncreasesInventoryQuantity` (Nhập kho tăng tồn) -> **PASSED**
2. `Test2_StockOut_DecreasesInventoryQuantity` (Xuất kho giảm tồn) -> **PASSED**
3. `Test3_StockOut_CannotExceedAvailableStock` (Chặn xuất vượt tồn) -> **PASSED**
4. `Test4_StockOperation_QuantityMustBeGreaterThanZero (0)` (Chặn số lượng = 0) -> **PASSED**
5. `Test4_StockOperation_QuantityMustBeGreaterThanZero (-10)` (Chặn số lượng âm) -> **PASSED**
6. `Test5_StockReceipt_CannotBeConfirmedTwice` (Chặn duyệt phiếu nhập 2 lần) -> **PASSED**
7. `Test6_ProductCode_MustBeUnique` (Chặn trùng mã linh kiện) -> **PASSED**
8. `Test7_StaffRole_CannotPerformAdminOnlyActions` (Kiểm tra phân quyền Admin/Staff) -> **PASSED**
9. `Test8_FailedTransaction_DoesNotChangeInventory` (Kiểm tra Rollback Transaction khi có lỗi) -> **PASSED**
10. `Test9_ConcurrentStockIssues_CannotCauseNegativeInventory` (Kiểm tra Race Condition không âm kho) -> **PASSED**

---

## 5. ĐIỂM NỔI BẬT ĐƯA VÀO CV ỨNG TUYỂN
- **ACID Transaction & Concurrency Control:** Am hiểu việc sử dụng `IDbContextTransaction` và kiểm soát đồng thời để đảm bảo tính toàn vẹn dữ liệu kế toán kho.
- **Multi-Location Inventory Modeling:** Nắm vững thiết kế cơ sở dữ liệu quan hệ quản lý tồn kho theo cặp `(ProductId, LocationId)`.
- **Clean Architecture & Separation of Concerns:** Tách biệt rõ ràng Controller, Service Layer, ViewModels và DbContext.
- **Automated Testing:** Có tư duy kiểm thử phần mềm nghiêm túc với xUnit và Moq.
