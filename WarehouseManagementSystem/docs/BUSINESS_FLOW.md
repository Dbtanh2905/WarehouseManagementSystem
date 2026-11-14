# QUY TRÌNH NGHIỆP VỤ - WAREHOUSE MANAGEMENT SYSTEM (WMS)

## 1. Giới thiệu
Tài liệu này mô tả chi tiết các luồng nghiệp vụ cốt lõi trong hệ thống quản lý kho linh kiện điện tử:
1. Quy trình quản lý Master Data (Linh kiện, Danh mục, Nhà cung cấp, Vị trí kho).
2. Quy trình Nhập kho (Stock In).
3. Quy trình Xuất kho (Stock Out).
4. Quy trình Quản lý và Cảnh báo Tồn kho (Inventory Monitoring).
5. Quy trình Sổ cái Biến động kho (Audit Trail).

---

## 2. Phân quyền người dùng (Role-Based Access Control)

| Chức năng | Staff (Nhân viên kho) | Admin (Quản lý kho) |
| :--- | :---: | :---: |
| Đăng nhập / Đổi mật khẩu | ✅ | ✅ |
| Xem Dashboard tổng quan | ✅ | ✅ |
| Xem danh mục, NCC, vị trí, sản phẩm | ✅ | ✅ |
| Thêm / Sửa danh mục, NCC, vị trí, sản phẩm | ❌ | ✅ |
| Xem danh sách tồn kho & Vị trí | ✅ | ✅ |
| Lập phiếu nhập kho (trạng thái Nháp) | ✅ | ✅ |
| Sửa / Hủy phiếu nhập kho (trạng thái Nháp) | ✅ | ✅ |
| **Xác nhận nhập kho (Confirmed)** | ❌ | ✅ |
| Lập phiếu xuất kho (trạng thái Nháp) | ✅ | ✅ |
| Sửa / Hủy phiếu xuất kho (trạng thái Nháp) | ✅ | ✅ |
| **Xác nhận xuất kho (Confirmed)** | ❌ | ✅ |
| Xem sổ cái lịch sử biến động kho | ✅ | ✅ |
| Xem & Xuất báo cáo CSV | ✅ | ✅ |
| Quản lý tài khoản người dùng | ❌ | ✅ |

---

## 3. Quy trình Nhập kho (Stock In Flow)

```mermaid
flowchart TD
    A([Bắt đầu: Hàng về từ NCC]) --> B[Staff / Admin tạo Phiếu nhập kho]
    B --> C[Nhập thông tin chung: NCC, Ngày nhập, Ghi chú]
    C --> D[Thêm các dòng linh kiện: Sản phẩm, Vị trí kệ, Số lượng > 0]
    D --> E[Lưu phiếu ở trạng thái 'Draft - Nháp']
    E --> F{Kiểm tra thực tế hàng nhập}
    F -->|Sai lệch / Hủy| G[Sửa thông tin hoặc Hủy phiếu 'Cancelled']
    F -->|Khớp thông tin| H[Admin nhấn 'Xác nhận nhập kho']
    H --> I[Bắt đầu Database Transaction]
    I --> J[Cập nhật trạng thái phiếu -> 'Confirmed']
    J --> K[Tìm hoặc Tạo bản ghi trong Inventories Product + Location]
    K --> L[Tăng số lượng: Quantity = Quantity + Qty_Nhập]
    L --> M[Ghi nhận vào StockTransactions: Type = In]
    M --> N{Thành công toàn bộ?}
    N -->|Có lỗi| O[Rollback Transaction & Báo lỗi]
    N -->|Thành công| P[Commit Transaction & Hoàn tất]
    P --> Q([Kết thúc: Kho đã cập nhật số lượng])
```

### Các quy tắc nghiệp vụ nhập kho:
- Mã phiếu tự sinh hoặc nhập thủ công nhưng **bắt buộc duy nhất** (VD: `PN-20261114-0001`).
- Số lượng nhập ở mỗi dòng bắt buộc $> 0$.
- Khi phiếu đang ở trạng thái `Draft`, chưa có bất kỳ sự thay đổi nào đối với số lượng tồn kho.
- Chỉ người dùng có vai trò `Admin` mới được phép xác nhận phiếu nhập.
- Phiếu đã xác nhận (`Confirmed`) hoặc đã hủy (`Cancelled`) sẽ bị khóa, không được phép chỉnh sửa hoặc xác nhận lại lần 2.

---

## 4. Quy trình Xuất kho (Stock Out Flow)

```mermaid
flowchart TD
    A([Bắt đầu: Yêu cầu cấp phát linh kiện]) --> B[Staff / Admin tạo Phiếu xuất kho]
    B --> C[Nhập thông tin: Ngày xuất, Mục đích cấp phát chuyền sx]
    C --> D[Chọn Linh kiện, Vị trí lấy hàng, Số lượng xuất > 0]
    D --> E[Hệ thống kiểm tra tồn kho hiện tại tại vị trí đó]
    E -->|Không đủ tồn| F[Cảnh báo không đủ số lượng tồn tại vị trí]
    E -->|Đủ tồn khả dụng| G[Lưu phiếu ở trạng thái 'Draft - Nháp']
    G --> H{Admin phê duyệt xuất kho}
    H -->|Không duyệt| I[Hủy phiếu 'Cancelled']
    H -->|Phê duyệt| J[Bắt đầu Database Transaction]
    J --> K[Lock hoặc đọc lại tồn kho hiện thời của từng dòng]
    K --> L{Tồn kho hiện tại >= Số lượng xuất?}
    L -->|Không đủ do giao dịch khác vừa xuất| M[Rollback & Báo lỗi Concurrency / Không đủ tồn]
    L -->|Đủ tồn kho| N[Cập nhật trạng thái phiếu -> 'Confirmed']
    N --> O[Giảm tồn kho: Quantity = Quantity - Qty_Xuất]
    O --> P[Ghi nhận vào StockTransactions: Type = Out]
    P --> Q[Commit Transaction]
    Q --> R([Kết thúc: Kho đã trừ số lượng xuất])
```

### Các quy tắc nghiệp vụ xuất kho:
- Kiểm tra tồn kho tại vị trí xuất: Nếu vị trí A01 của sản phẩm CPU chỉ có 5 chiếc, không được phép xuất 6 chiếc từ vị trí A01.
- Số lượng xuất $> 0$.
- Đảm bảo tính toàn vẹn: Xử lý trong một `IDbContextTransaction`.
- Ngăn chặn Race Condition: Kiểm tra số lượng thực tế ngay tại thời điểm xác nhận (Commit), đảm bảo số lượng tồn kho không bao giờ âm.

---

## 5. Quy trình Quản lý tồn kho & Cảnh báo mức tồn tối thiểu

- **Tổng tồn kho của một sản phẩm** = Tổng số lượng của sản phẩm đó trên tất cả các vị trí lưu trữ (`SUM(Quantity)` theo `ProductId`).
- **Phân loại trạng thái:**
  - `Hết hàng (Out of Stock)`: Tổng tồn kho $= 0$.
  - `Sắp hết hàng (Low Stock)`: Tổng tồn kho $> 0$ và $\le$ `MinimumStock`.
  - `Bình thường (In Stock)`: Tổng tồn kho $>$ `MinimumStock`.
- Cảnh báo trực quan trên Dashboard và trang Inventory để nhân viên kịp thời lên kế hoạch đặt hàng từ nhà cung cấp.
