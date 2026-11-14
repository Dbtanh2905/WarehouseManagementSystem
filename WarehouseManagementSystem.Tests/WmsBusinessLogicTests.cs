using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using WarehouseManagementSystem.Controllers;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.Services;
using WarehouseManagementSystem.ViewModels;
using Xunit;

namespace WarehouseManagementSystem.Tests
{
    public class WmsBusinessLogicTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<StockReceiptService>> _receiptLoggerMock;
        private readonly Mock<ILogger<StockIssueService>> _issueLoggerMock;
        private readonly StockReceiptService _receiptService;
        private readonly StockIssueService _issueService;

        public WmsBusinessLogicTests()
        {
            // Thiết lập SQLite In-Memory Database hỗ trợ Relational & Transactions đầy đủ
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // Seed master data cơ sở
            SeedBaseData();

            _receiptLoggerMock = new Mock<ILogger<StockReceiptService>>();
            _issueLoggerMock = new Mock<ILogger<StockIssueService>>();

            _receiptService = new StockReceiptService(_context, _receiptLoggerMock.Object);
            _issueService = new StockIssueService(_context, _issueLoggerMock.Object);
        }

        private void SeedBaseData()
        {
            var category = new Category { Id = 1, CategoryName = "Vi xử lý", Description = "CPU các loại" };
            var supplier = new Supplier { Id = 1, SupplierName = "Intel Corporation", Phone = "0123456789" };
            var location1 = new WarehouseLocation { Id = 1, LocationCode = "A01", LocationName = "Kệ A01" };
            var location2 = new WarehouseLocation { Id = 2, LocationCode = "A02", LocationName = "Kệ A02" };

            var product = new Product
            {
                Id = 1,
                ProductCode = "CPU-I7-13700",
                ProductName = "Intel Core i7 13700",
                CategoryId = 1,
                SupplierId = 1,
                Unit = "Chiếc",
                MinimumStock = 10,
                IsActive = true
            };

            var userAdmin = new ApplicationUser
            {
                Id = "admin-id",
                UserName = "admin@wms.com",
                Email = "admin@wms.com",
                FullName = "Admin Quản Trị"
            };

            var userStaff = new ApplicationUser
            {
                Id = "staff-id",
                UserName = "staff@wms.com",
                Email = "staff@wms.com",
                FullName = "Staff Nhân Viên"
            };

            _context.Categories.Add(category);
            _context.Suppliers.Add(supplier);
            _context.WarehouseLocations.AddRange(location1, location2);
            _context.Products.Add(product);
            _context.Users.AddRange(userAdmin, userStaff);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        // ==========================================
        // 1. NHẬP KHO LÀM TĂNG SỐ LƯỢNG TỒN
        // ==========================================
        [Fact]
        public async Task Test1_StockIn_IncreasesInventoryQuantity()
        {
            // Arrange
            var model = new StockReceiptCreateViewModel
            {
                ReceiptCode = "PN-TEST-0001",
                ReceiptDate = DateTime.UtcNow,
                SupplierId = 1,
                Details = new List<StockReceiptDetailItemInput>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 50 }
                }
            };

            var (createSuccess, _, receiptId) = await _receiptService.CreateReceiptAsync(model, "staff-id");
            Assert.True(createSuccess);

            // Tồn kho trước khi xác nhận phải là 0
            var invBefore = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.Null(invBefore);

            // Act: Admin xác nhận phiếu nhập
            var (confirmSuccess, _) = await _receiptService.ConfirmReceiptAsync(receiptId, "admin-id");

            // Assert
            Assert.True(confirmSuccess);
            var invAfter = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.NotNull(invAfter);
            Assert.Equal(50, invAfter.Quantity);

            // Kiểm tra lịch sử giao dịch được ghi nhận
            var tx = await _context.StockTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "PN-TEST-0001");
            Assert.NotNull(tx);
            Assert.Equal(TransactionType.In, tx.TransactionType);
            Assert.Equal(50, tx.Quantity);
        }

        // ==========================================
        // 2. XUẤT KHO LÀM GIẢM SỐ LƯỢNG TỒN
        // ==========================================
        [Fact]
        public async Task Test2_StockOut_DecreasesInventoryQuantity()
        {
            // Arrange: Thiết lập sẵn tồn kho 100 chiếc tại vị trí A01
            _context.Inventories.Add(new Inventory
            {
                ProductId = 1,
                LocationId = 1,
                Quantity = 100,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var model = new StockIssueCreateViewModel
            {
                IssueCode = "PX-TEST-0001",
                IssueDate = DateTime.UtcNow,
                Note = "Xuất cấp dây chuyền",
                Details = new List<StockIssueDetailItemInput>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 30 }
                }
            };

            var (createSuccess, _, issueId) = await _issueService.CreateIssueAsync(model, "staff-id");
            Assert.True(createSuccess);

            // Act: Admin xác nhận xuất kho
            var (confirmSuccess, _) = await _issueService.ConfirmIssueAsync(issueId, "admin-id");

            // Assert
            Assert.True(confirmSuccess);
            var invAfter = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.NotNull(invAfter);
            Assert.Equal(70, invAfter.Quantity); // 100 - 30 = 70

            // Kiểm tra transaction Type = Out
            var tx = await _context.StockTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "PX-TEST-0001");
            Assert.NotNull(tx);
            Assert.Equal(TransactionType.Out, tx.TransactionType);
            Assert.Equal(30, tx.Quantity);
        }

        // ==========================================
        // 3. KHÔNG CHO PHÉP XUẤT VƯỢT TỒN KHO
        // ==========================================
        [Fact]
        public async Task Test3_StockOut_CannotExceedAvailableStock()
        {
            // Arrange: Tồn kho chỉ có 20 chiếc
            _context.Inventories.Add(new Inventory
            {
                ProductId = 1,
                LocationId = 1,
                Quantity = 20,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Cố tình xuất 25 chiếc (vượt quá 20 chiếc)
            var model = new StockIssueCreateViewModel
            {
                IssueCode = "PX-OVER-0001",
                IssueDate = DateTime.UtcNow,
                Details = new List<StockIssueDetailItemInput>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 25 }
                }
            };

            // Act
            var (createSuccess, createMsg, _) = await _issueService.CreateIssueAsync(model, "staff-id");

            // Assert: Hệ thống phải từ chối ngay từ bước lập phiếu hoặc xác nhận
            Assert.False(createSuccess);
            Assert.Contains("không đủ để xuất", createMsg);

            // Kiểm tra tồn kho vẫn nguyên vẹn 20
            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.Equal(20, inv?.Quantity);
        }

        // ==========================================
        // 4. KHÔNG CHO PHÉP SỐ LƯỢNG NHẬP/XUẤT <= 0
        // ==========================================
        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task Test4_StockOperation_QuantityMustBeGreaterThanZero(int invalidQty)
        {
            // Test Nhập kho với số lượng <= 0
            var receiptModel = new StockReceiptCreateViewModel
            {
                ReceiptCode = "PN-INVALID",
                ReceiptDate = DateTime.UtcNow,
                SupplierId = 1,
                Details = new List<StockReceiptDetailItemInput>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = invalidQty }
                }
            };

            var (rSuccess, rMsg, _) = await _receiptService.CreateReceiptAsync(receiptModel, "staff-id");
            Assert.False(rSuccess);
            Assert.Contains("lớn hơn 0", rMsg);

            // Test Xuất kho với số lượng <= 0
            var issueModel = new StockIssueCreateViewModel
            {
                IssueCode = "PX-INVALID",
                IssueDate = DateTime.UtcNow,
                Details = new List<StockIssueDetailItemInput>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = invalidQty }
                }
            };

            var (iSuccess, iMsg, _) = await _issueService.CreateIssueAsync(issueModel, "staff-id");
            Assert.False(iSuccess);
            Assert.Contains("lớn hơn 0", iMsg);
        }

        // ==========================================
        // 5. KHÔNG CHO PHÉP XÁC NHẬN MỘT PHIẾU NHIỀU LẦN
        // ==========================================
        [Fact]
        public async Task Test5_StockReceipt_CannotBeConfirmedTwice()
        {
            // Arrange
            var model = new StockReceiptCreateViewModel
            {
                ReceiptCode = "PN-TWICE-001",
                ReceiptDate = DateTime.UtcNow,
                SupplierId = 1,
                Details = new List<StockReceiptDetailItemInput>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 10 }
                }
            };

            var (_, _, receiptId) = await _receiptService.CreateReceiptAsync(model, "staff-id");

            // Xác nhận lần 1 -> Thành công
            var (firstSuccess, _) = await _receiptService.ConfirmReceiptAsync(receiptId, "admin-id");
            Assert.True(firstSuccess);

            // Act: Cố tình gọi xác nhận lần 2
            var (secondSuccess, secondMsg) = await _receiptService.ConfirmReceiptAsync(receiptId, "admin-id");

            // Assert: Bị từ chối
            Assert.False(secondSuccess);
            Assert.Contains("trước đó", secondMsg);

            // Tồn kho chỉ được cộng 1 lần duy nhất (10 chiếc, không phải 20)
            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.Equal(10, inv?.Quantity);
        }

        // ==========================================
        // 6. KHÔNG CHO PHÉP MÃ SẢN PHẨM TRÙNG NHAU
        // ==========================================
        [Fact]
        public async Task Test6_ProductCode_MustBeUnique()
        {
            // Sản phẩm 'CPU-I7-13700' đã có trong SeedBaseData
            var duplicateProduct = new Product
            {
                ProductCode = "CPU-I7-13700", // Trùng mã
                ProductName = "Một linh kiện CPU khác",
                CategoryId = 1,
                SupplierId = 1,
                Unit = "Cái",
                MinimumStock = 5
            };

            // Trong SQL Server / Database, Unique Index sẽ ném ngoại lệ DbUpdateException
            _context.Products.Add(duplicateProduct);
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await _context.SaveChangesAsync();
            });
        }

        // ==========================================
        // 7. PHÂN QUYỀN: STAFF KHÔNG ĐƯỢC DUYỆT PHIẾU HOẶC QUẢN TRỊ NGƯỜI DÙNG
        // ==========================================
        [Fact]
        public void Test7_StaffRole_CannotPerformAdminOnlyActions()
        {
            // Kiểm tra controller attribute bảo vệ phân quyền trên tầng Backend
            var confirmReceiptMethod = typeof(StockReceiptController).GetMethod("Confirm");
            Assert.NotNull(confirmReceiptMethod);
            var receiptAuthorizeAttr = confirmReceiptMethod.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(receiptAuthorizeAttr);
            Assert.Equal("Admin", receiptAuthorizeAttr.Roles);

            var confirmIssueMethod = typeof(StockIssueController).GetMethod("Confirm");
            Assert.NotNull(confirmIssueMethod);
            var issueAuthorizeAttr = confirmIssueMethod.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(issueAuthorizeAttr);
            Assert.Equal("Admin", issueAuthorizeAttr.Roles);

            var usersActionMethod = typeof(AccountController).GetMethod("Users");
            Assert.NotNull(usersActionMethod);
            var usersAuthorizeAttr = usersActionMethod.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(usersAuthorizeAttr);
            Assert.Equal("Admin", usersAuthorizeAttr.Roles);
        }

        // ==========================================
        // 8. GIAO DỊCH THẤT BẠI KHÔNG LÀM THAY ĐỔI TỒN KHO (TRANSACTION ROLLBACK)
        // ==========================================
        [Fact]
        public async Task Test8_FailedTransaction_DoesNotChangeInventory()
        {
            // Arrange: Tồn kho đang có 50 chiếc
            _context.Inventories.Add(new Inventory
            {
                ProductId = 1,
                LocationId = 1,
                Quantity = 50,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Tạo phiếu xuất hợp lệ lúc đầu
            var issue = new StockIssue
            {
                IssueCode = "PX-ROLLBACK-001",
                IssueDate = DateTime.UtcNow,
                CreatedById = "staff-id",
                Status = IssueStatus.Draft,
                IssueDetails = new List<StockIssueDetail>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 40 },
                    // Dòng thứ 2 cố tình đòi xuất 20 chiếc nữa (tổng 40 + 20 = 60 > 50 tồn kho)
                    new() { ProductId = 1, LocationId = 1, Quantity = 20 }
                }
            };
            _context.StockIssues.Add(issue);
            await _context.SaveChangesAsync();

            // Act: Gọi Confirm -> Phải thất bại và Rollback toàn bộ
            var (success, msg) = await _issueService.ConfirmIssueAsync(issue.Id, "admin-id");

            // Assert
            Assert.False(success);
            Assert.Contains("không đủ để xuất", msg);

            // Kiểm tra tồn kho vẫn nguyên vẹn 50 chiếc (không bị trừ dở 40 chiếc ở dòng 1)
            var inv = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.Equal(50, inv?.Quantity);

            // Trạng thái phiếu vẫn là Draft (chưa bị đổi thành Confirmed)
            var issueReloaded = await _context.StockIssues.AsNoTracking().FirstOrDefaultAsync(i => i.Id == issue.Id);
            Assert.Equal(IssueStatus.Draft, issueReloaded?.Status);
        }

        // ==========================================
        // 9. HAI YÊU CẦU XUẤT KHO ĐỒNG THỜI KHÔNG THỂ LÀM ÂM TỒN KHO
        // ==========================================
        [Fact]
        public async Task Test9_ConcurrentStockIssues_CannotCauseNegativeInventory()
        {
            // Arrange: Tồn kho ban đầu có 15 chiếc
            _context.Inventories.Add(new Inventory
            {
                ProductId = 1,
                LocationId = 1,
                Quantity = 15,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Tạo 2 phiếu xuất nháp khác nhau, mỗi phiếu đòi xuất 10 chiếc
            // (10 + 10 = 20 > 15) -> Chỉ 1 phiếu được duyệt thành công, phiếu kia phải bị từ chối!
            var issue1 = new StockIssue
            {
                IssueCode = "PX-CONCURRENT-1",
                IssueDate = DateTime.UtcNow,
                CreatedById = "staff-id",
                Status = IssueStatus.Draft,
                IssueDetails = new List<StockIssueDetail>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 10 }
                }
            };
            var issue2 = new StockIssue
            {
                IssueCode = "PX-CONCURRENT-2",
                IssueDate = DateTime.UtcNow,
                CreatedById = "staff-id",
                Status = IssueStatus.Draft,
                IssueDetails = new List<StockIssueDetail>
                {
                    new() { ProductId = 1, LocationId = 1, Quantity = 10 }
                }
            };

            _context.StockIssues.AddRange(issue1, issue2);
            await _context.SaveChangesAsync();

            // Act: Lần lượt xác nhận 2 phiếu
            var res1 = await _issueService.ConfirmIssueAsync(issue1.Id, "admin-id");
            var res2 = await _issueService.ConfirmIssueAsync(issue2.Id, "admin-id");

            // Assert: Một phiếu thành công, một phiếu thất bại
            Assert.True(res1.Success);
            Assert.False(res2.Success);
            Assert.Contains("không đủ để xuất", res2.Message);

            // Tồn kho cuối cùng phải là 5 (15 - 10 = 5), tuyệt đối không bao giờ bị âm (-5)
            var finalInv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1 && i.LocationId == 1);
            Assert.NotNull(finalInv);
            Assert.Equal(5, finalInv.Quantity);
            Assert.True(finalInv.Quantity >= 0);
        }
    }
}
