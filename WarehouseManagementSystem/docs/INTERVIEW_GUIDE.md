# HƯỚNG DẪN TRẢ LỜI PHỎNG VẤN IT MES / PHẦN MỀM SẢN XUẤT
### Dành cho sinh viên ứng tuyển vị trí Kỹ sư Phần mềm / IT MES (Manufacturing Execution System) tại các tập đoàn sản xuất điện tử (như Foxconn, Luxshare, Samsung, Compal, Pegatron)

---

## 1. GIỚI THIỆU TỔNG QUAN VỀ DỰ ÁN KHI ĐƯỢC HỎI
> **Câu hỏi:** *"Hãy giới thiệu một dự án tiêu biểu mà em đã thực hiện?"*

**Cách trả lời ghi điểm:**
> "Em xin phép giới thiệu dự án **WMS - Hệ thống Quản lý Kho Linh kiện Điện tử** mà em phát triển bằng ASP.NET Core MVC và Microsoft SQL Server. Dự án mô phỏng hoạt động kho thực tế của một nhà máy sản xuất bo mạch điện tử với quy mô đa vị trí kệ (**Multi-location inventory**).
> 
> Trong dự án này, em đã giải quyết 3 bài toán kỹ thuật cốt lõi:
> 1. **Toàn vẹn dữ liệu trong giao dịch kho (ACID Transactions):** Đảm bảo việc duyệt phiếu nhập/xuất, cập nhật số lượng tồn kho theo từng cặp `(ProductId, LocationId)` và ghi sổ cái biến động kho (`StockTransactions` - Audit Trail) luôn diễn ra nguyên khối trong một `IDbContextTransaction`.
> 2. **Kiểm soát đồng thời và chống âm tồn kho (Concurrency Control):** Ngăn chặn trường hợp hai nhân viên cùng xuất một loại linh kiện tại cùng một vị trí dẫn đến xuất vượt số lượng tồn thực tế.
> 3. **Phân quyền và bảo mật (RBAC):** Ứng dụng ASP.NET Core Identity với hai vai trò `Admin` (quản trị, phê duyệt phiếu) và `Staff` (lập phiếu nháp, theo dõi tồn kho) được kiểm soát chặt chẽ ở backend service.
> Dự án được viết unit test tự động bằng xUnit với 100% test case nghiệp vụ cốt lõi đều đạt kết quả pass."

---

## 2. GIẢI THÍCH KIẾN TRÚC & CÔNG NGHỆ

### 2.1. Tại sao dùng Kiến trúc 3 tầng (Controller -> Service -> Data)?
- **Controller:** Chỉ đóng vai trò tiếp nhận HTTP Request, kiểm tra `ModelState.IsValid`, gọi Service và trả về View / JSON tương ứng. Không chứa business logic phức tạp.
- **Service Layer (`IInventoryService`, `IStockReceiptService`, `IStockIssueService`,...):** Nơi chứa toàn bộ nghiệp vụ kiểm tra tồn kho, mở transaction, tính toán định mức tồn tối thiểu và ghi sổ cái. Việc tách Service giúp code dễ bảo trì và đặc biệt là **có thể viết Unit Test tự động mà không cần khởi động Web UI**.
- **Data Layer (`ApplicationDbContext`):** Cấu hình quan hệ bảng, Fluent API, Unique Index và Check Constraints.

### 2.2. Entity Framework Core Code-First & Migrations
- Code-First cho phép định nghĩa lược đồ cơ sở dữ liệu hoàn toàn bằng C# classes.
- EF Migrations giúp quản lý lịch sử thay đổi schema (như git commit cho database), cho phép dễ dàng cập nhật database trên server mà không cần chạy file `.sql` thủ công.
- Lưu ý hiệu năng: Sử dụng `.AsNoTracking()` cho các truy vấn chỉ đọc để EF Core không tốn bộ nhớ tracking trạng thái của entity.

### 2.3. Database Transactions (`IDbContextTransaction`)
> **Câu hỏi:** *"Tại sao cần Database Transaction trong hàm xác nhận phiếu nhập/xuất kho?"*

**Trả lời:**
> "Trong nghiệp vụ kho, thao tác xác nhận một phiếu xuất gồm 3 bước:
> 1. Đổi trạng thái phiếu từ `Draft` sang `Confirmed`.
> 2. Trừ số lượng tồn kho của linh kiện tại vị trí tương ứng trong bảng `Inventories`.
> 3. Thêm bản ghi vào bảng sổ cái `StockTransactions` để phục vụ đối soát và kiểm toán.
> 
> Nếu không dùng transaction, giả sử bước 1 và 2 thành công nhưng bước 3 bị lỗi (mất mạng, ngắt kết nối database), dữ liệu sẽ rơi vào trạng thái không nhất quán: kho bị mất hàng nhưng sổ cái không có dấu vết giao dịch.
> Bằng cách bọc toàn bộ trong `using var transaction = await _context.Database.BeginTransactionAsync()`, nếu có bất kỳ bước nào thất bại, hệ thống sẽ tự động `RollbackAsync()`, đảm bảo dữ liệu trở về trạng thái toàn vẹn ban đầu."

### 2.4. Ngăn chặn Race Condition và Xuất âm tồn kho
> **Câu hỏi:** *"Nếu có 2 người dùng bấm nút xuất 10 chiếc linh kiện tại vị trí A01 cùng lúc, trong khi kho chỉ còn 15 chiếc thì hệ thống xử lý thế nào?"*

**Trả lời:**
> "Hệ thống có cơ chế bảo vệ 3 lớp:
> 1. **Lớp Giao diện & JavaScript:** Kiểm tra số lượng nhập vào ô input không được vượt quá số lượng tồn khả dụng hiện thời của vị trí đó.
> 2. **Lớp Service (Application Layer):** Trong Transaction, hệ thống thực hiện đọc lại số lượng tồn kho thực tế (`inventory.Quantity`) và so sánh với tổng số lượng xuất yêu cầu (`TotalRequired`). Nếu `inventory.Quantity < TotalRequired`, giao dịch lập tức bị hủy bỏ và Rollback kèm thông báo rõ ràng cho người dùng. Đồng thời bảng `Inventories` có cột `RowVersion` kiểu timestamp để phát hiện xung đột đồng thời (`DbUpdateConcurrencyException`).
> 3. **Lớp Database (Database Constraint):** Bảng `Inventories` có ràng buộc kiểm tra cấp database: `CONSTRAINT CK_Inventory_Quantity_NonNegative CHECK (Quantity >= 0)`. Dù có bất kỳ lỗi logic nào ở tầng trên, SQL Server cũng sẽ từ chối lưu và ném lỗi nếu số lượng nhỏ hơn 0."

---

## 3. CÁC CÂU HỎI NHÀ TUYỂN DỤNG IT MES HAY HỎI

### Câu 1: Hệ thống MES trong sản xuất khác gì so với hệ thống WMS?
- **WMS (Warehouse Management System):** Tập trung vào việc quản lý nguyên vật liệu lưu kho, vị trí kệ (Rack/Bin), nhập hàng từ nhà cung cấp (IQC), cấp phát vật tư cho sản xuất, và kiểm kê tồn kho.
- **MES (Manufacturing Execution System):** Quản lý quá trình thực thi sản xuất trên chuyền (Shop Floor): từ khi nhận linh kiện từ kho WMS, đưa vào máy dán bề mặt (SMT - Surface Mount Technology), máy hàn sóng, kiểm tra quang học tự động (AOI), kiểm tra chức năng (FCT), dán nhãn Serial Number / Barcode cho từng bo mạch (Traceability) cho đến khi ra thành phẩm.
- **Điểm giao thoa:** WMS cấp phát đúng linh kiện (đúng Part Number, đúng Lot number) cho MES tại đầu chuyền sản xuất.

### Câu 2: Tại sao tồn kho phải quản lý theo cặp (ProductId, LocationId) thay vì lưu cột Quantity ngay trong bảng Product?
- Trong thực tế nhà máy, một loại chip CPU hay cuộn tụ điện có thể được cất ở nhiều kệ khác nhau (kệ phòng sạch A01, kệ phụ tầng hai B03).
- Nếu chỉ lưu một cột `Quantity` trong bảng `Products`, ta chỉ biết tổng số lượng mà không thể biết linh kiện đang nằm chính xác ở vị trí nào trong nhà kho rộng hàng nghìn mét vuông.
- Mô hình `Inventories(ProductId, LocationId, Quantity)` với Unique Constraint trên cặp `(ProductId, LocationId)` giải quyết triệt để bài toán định vị chính xác vật tư.

### Câu 3: Làm sao để xuất file CSV tiếng Việt mà không bị lỗi font khi mở bằng Microsoft Excel?
- Khi ghi chuỗi tiếng Việt Unicode UTF-8 ra file CSV, nếu mở bằng Excel trên Windows, Excel thường dùng mã ANSI mặc định và bị vỡ font.
- Để khắc phục, cần ghi thêm **3 byte UTF-8 BOM (Byte Order Mark - `EF BB BF`)** vào đầu file (`Encoding.UTF8.GetPreamble()`). Nhờ có BOM, Microsoft Excel tự động nhận diện đúng bảng mã UTF-8 và hiển thị tiếng Việt hoàn hảo.

---

## 4. NHỮNG PHẦN SINH VIÊN CẦN NẮM VỮNG VÀ TỰ TÌM HIỂU THÊM
1. **Kiểm tra Barcode / QR Code Scanner:** Trong nhà máy thực tế, nhân viên kho thường dùng máy quét mã vạch cầm tay (Zebra Scanner) gửi tín hiệu qua cổng COM/USB mô phỏng bàn phím hoặc gửi qua Web API.
2. **First-In, First-Out (FIFO) & Quản lý Lot/Batch:** Trong ngành điện tử, linh kiện bán dẫn có thời hạn bảo quản (MSL - Moisture Sensitivity Level). Hãy tìm hiểu thêm quy tắc xuất linh kiện có Date Code hoặc Lot nhập trước ra dùng trước (FIFO).
3. **Tích hợp MES / ERP:** WMS thường giao tiếp với ERP (như SAP) qua giao thức RESTful Web API hoặc Message Broker (như RabbitMQ, Kafka) để nhận Lệnh sản xuất (Work Order) và Đơn hàng nhập khẩu (PO - Purchase Order).
