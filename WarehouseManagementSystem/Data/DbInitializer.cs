using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = ["Admin", "Staff"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            string adminPassword = configuration["DemoAccounts:AdminPassword"] ?? "Admin@123456";
            string staffPassword = configuration["DemoAccounts:StaffPassword"] ?? "Staff@123456";

            var adminUser = await userManager.FindByEmailAsync("admin@wms.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@wms.com",
                    Email = "admin@wms.com",
                    FullName = "Nguyễn Văn Quản Trị",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Đồng bộ lại mật khẩu chính xác nếu tài khoản đã tồn tại
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
            }

            var staffUser = await userManager.FindByEmailAsync("staff@wms.com");
            if (staffUser == null)
            {
                staffUser = new ApplicationUser
                {
                    UserName = "staff@wms.com",
                    Email = "staff@wms.com",
                    FullName = "Trần Thị Nhân Viên",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(staffUser, staffPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, "Staff");
                }
            }
            else
            {
                // Đồng bộ lại mật khẩu chính xác nếu tài khoản đã tồn tại
                var token = await userManager.GeneratePasswordResetTokenAsync(staffUser);
                await userManager.ResetPasswordAsync(staffUser, token, staffPassword);
            }

            // 3. Seed Categories (5 categories)
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new() { CategoryName = "Vi xử lý & CPU", Description = "Các dòng CPU công nghiệp, vi xử lý trung tâm x86 và ARM" },
                    new() { CategoryName = "Bộ nhớ & RAM", Description = "Module RAM DDR4, DDR5, bộ nhớ đệm và chip Flash NAND" },
                    new() { CategoryName = "Bo mạch & PCB", Description = "Bo mạch chủ công nghiệp, mạch in nhiều lớp và socket" },
                    new() { CategoryName = "Mô-đun Nguồn & Biến áp", Description = "Module nguồn xung DC-DC, IC ổn áp và bộ chuyển đổi AC-DC" },
                    new() { CategoryName = "Linh kiện thụ động & IC", Description = "Tụ gốm nhiều lớp (MLCC), điện trở dán (SMD), cuộn cảm và IC logic" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed Suppliers (5 suppliers)
            if (!context.Suppliers.Any())
            {
                var suppliers = new List<Supplier>
                {
                    new() { SupplierName = "Intel Semiconductor Co.", Phone = "024-3788-1111", Email = "contact@intel-supply.com", Address = "Khu Công Nghệ Cao Hòa Lạc, Hà Nội" },
                    new() { SupplierName = "Samsung Electronics Components", Phone = "0222-383-9999", Email = "b2b@samsung-vn.com", Address = "KCN Yên Phong, Bắc Ninh" },
                    new() { SupplierName = "Foxconn Interconnect Technology", Phone = "0204-386-8888", Email = "procurement@foxconn.com.vn", Address = "KCN Quang Châu, Bắc Giang" },
                    new() { SupplierName = "Texas Instruments Industrial", Phone = "028-3824-7777", Email = "sales@ti-vietnam.com", Address = "Khu Chế Xuất Tân Thuận, Quận 7, TP.HCM" },
                    new() { SupplierName = "Murata Manufacturing Global", Phone = "024-3955-4444", Email = "support@murata.com.vn", Address = "KCN Thăng Long I, Đông Anh, Hà Nội" }
                };
                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
            }

            // 5. Seed Warehouse Locations (6 locations)
            if (!context.WarehouseLocations.Any())
            {
                var locations = new List<WarehouseLocation>
                {
                    new() { LocationCode = "LOC-A01", LocationName = "Kệ A01 - Khoản vi xử lý CPU", Description = "Kệ chuyên dụng kiểm soát nhiệt độ phòng sạch", IsActive = true },
                    new() { LocationCode = "LOC-A02", LocationName = "Kệ A02 - Khay Module RAM", Description = "Kệ tầng 2 chống tĩnh điện ESD cho RAM", IsActive = true },
                    new() { LocationCode = "LOC-B01", LocationName = "Kệ B01 - Kệ Bo mạch chủ PCB", Description = "Kệ kích thước lớn có đệm xốp chống va đập", IsActive = true },
                    new() { LocationCode = "LOC-B02", LocationName = "Kệ B02 - Kệ Module Nguồn điện", Description = "Kệ chịu tải trọng nặng dành cho nguồn công suất lớn", IsActive = true },
                    new() { LocationCode = "LOC-C01", LocationName = "Kệ C01 - Khay IC & Vi mạch dán", Description = "Tủ khay phân loại chi tiết IC SOP/QFP", IsActive = true },
                    new() { LocationCode = "LOC-C02", LocationName = "Kệ C02 - Cuộn tụ điện & Linh kiện SMD", Description = "Kệ cuộn Reel dán SMD và tụ MLCC", IsActive = true }
                };
                await context.WarehouseLocations.AddRangeAsync(locations);
                await context.SaveChangesAsync();
            }

            // 6. Seed 20 Electronic Products
            if (!context.Products.Any())
            {
                var catList = await context.Categories.OrderBy(c => c.Id).ToListAsync();
                var supList = await context.Suppliers.OrderBy(s => s.Id).ToListAsync();

                var products = new List<Product>
                {
                    // Category 1: Vi xử lý & CPU (catList[0])
                    new() { ProductCode = "CPU-I7-13700", ProductName = "Intel Core i7-13700 16-Core LGA1700", CategoryId = catList[0].Id, SupplierId = supList[0].Id, Unit = "Chiếc", MinimumStock = 20, Description = "CPU kiến trúc Raptor Lake 16 nhân 24 luồng" },
                    new() { ProductCode = "CPU-I5-13400", ProductName = "Intel Core i5-13400 10-Core LGA1700", CategoryId = catList[0].Id, SupplierId = supList[0].Id, Unit = "Chiếc", MinimumStock = 30, Description = "CPU phổ thông cho máy tính nhúng công nghiệp" },
                    new() { ProductCode = "MCU-STM32F407", ProductName = "Vi điều khiển ARM Cortex-M4 STM32F407VGT6", CategoryId = catList[0].Id, SupplierId = supList[3].Id, Unit = "Con", MinimumStock = 100, Description = "Vi điều khiển nhúng 32-bit tần số 168MHz" },
                    new() { ProductCode = "MCU-ESP32-WROOM", ProductName = "Module Vi điều khiển WiFi/BLE ESP32-WROOM-32D", CategoryId = catList[0].Id, SupplierId = supList[2].Id, Unit = "Module", MinimumStock = 80, Description = "Chip tích hợp WiFi 802.11 b/g/n và Bluetooth 4.2" },

                    // Category 2: Bộ nhớ & RAM (catList[1])
                    new() { ProductCode = "RAM-DDR4-8GB", ProductName = "Samsung DDR4 8GB SODIMM 3200MHz", CategoryId = catList[1].Id, SupplierId = supList[1].Id, Unit = "Thanh", MinimumStock = 50, Description = "Module RAM DDR4 tiết kiệm năng lượng" },
                    new() { ProductCode = "RAM-DDR5-16GB", ProductName = "Samsung DDR5 16GB ECC UDIMM 4800MHz", CategoryId = catList[1].Id, SupplierId = supList[1].Id, Unit = "Thanh", MinimumStock = 30, Description = "Module RAM thế hệ mới có tự sửa lỗi ECC" },
                    new() { ProductCode = "NAND-128GB-UFS", ProductName = "Chip nhớ Flash NAND UFS 3.1 128GB", CategoryId = catList[1].Id, SupplierId = supList[1].Id, Unit = "Con", MinimumStock = 60, Description = "Bộ nhớ nhúng tốc độ đọc tuần tự lên đến 2100MB/s" },
                    new() { ProductCode = "EEPROM-AT24C256", ProductName = "Bộ nhớ EEPROM I2C 256Kbit SOP-8", CategoryId = catList[1].Id, SupplierId = supList[3].Id, Unit = "Con", MinimumStock = 150, Description = "Bộ nhớ lưu trữ cấu hình thiết bị nhúng" },

                    // Category 3: Bo mạch & PCB (catList[2])
                    new() { ProductCode = "PCB-MAIN-ITX", ProductName = "Bo mạch chủ Mini-ITX Industrial Q670", CategoryId = catList[2].Id, SupplierId = supList[2].Id, Unit = "Bo", MinimumStock = 15, Description = "Mainboard công nghiệp hỗ trợ CPU Gen 12/13" },
                    new() { ProductCode = "PCB-CARRIER-COM", ProductName = "Carrier Board cho Module Compute COM-Express", CategoryId = catList[2].Id, SupplierId = supList[2].Id, Unit = "Bo", MinimumStock = 10, Description = "Bo mạch mở rộng giao tiếp PCIe và Ethernet" },
                    new() { ProductCode = "SOCK-LGA1700", ProductName = "Đế Socket CPU LGA1700 Foxconn SMD", CategoryId = catList[2].Id, SupplierId = supList[2].Id, Unit = "Cái", MinimumStock = 100, Description = "Đế gắn CPU lên bo mạch chính" },
                    new() { ProductCode = "CONN-PCIE-16X", ProductName = "Khe cắm PCI Express x16 Slot Connector", CategoryId = catList[2].Id, SupplierId = supList[2].Id, Unit = "Cái", MinimumStock = 120, Description = "Cổng kết nối card đồ họa và card mạng mở rộng" },

                    // Category 4: Mô-đun Nguồn & Biến áp (catList[3])
                    new() { ProductCode = "PWR-DCDC-12V5V", ProductName = "Module hạ áp Buck DC-DC 12V to 5V 5A", CategoryId = catList[3].Id, SupplierId = supList[3].Id, Unit = "Module", MinimumStock = 40, Description = "Chuyển đổi nguồn hiệu suất cao 95%" },
                    new() { ProductCode = "IC-REG-LM2596", ProductName = "IC ổn áp xung hạ áp LM2596S-5.0 TO-263", CategoryId = catList[3].Id, SupplierId = supList[3].Id, Unit = "Con", MinimumStock = 200, Description = "IC nguồn Switching 3A tích hợp cuộn dao động" },
                    new() { ProductCode = "PWR-ACDC-24V5A", ProductName = "Bộ nguồn tổ ong AC-DC 24V 5A 120W Din-rail", CategoryId = catList[3].Id, SupplierId = supList[2].Id, Unit = "Bộ", MinimumStock = 15, Description = "Nguồn cung cấp cho hệ thống PLC tủ điện" },
                    new() { ProductCode = "MOSFET-IRFZ44N", ProductName = "Transistor MOSFET công suất IRFZ44N 55V 49A", CategoryId = catList[3].Id, SupplierId = supList[3].Id, Unit = "Con", MinimumStock = 150, Description = "Khóa điện tử đóng cắt dòng cao cho mạch nguồn" },

                    // Category 5: Linh kiện thụ động & IC (catList[4])
                    new() { ProductCode = "CAP-MLCC-10UF", ProductName = "Tụ gốm dán MLCC 10uF 25V X7R 0805", CategoryId = catList[4].Id, SupplierId = supList[4].Id, Unit = "Cuộn", MinimumStock = 10, Description = "Cuộn 4000 con tụ dán lọc nhiễu đường nguồn" },
                    new() { ProductCode = "CAP-ELEC-470UF", ProductName = "Tụ hóa nhôm 470uF 35V Low-ESR 105C", CategoryId = catList[4].Id, SupplierId = supList[4].Id, Unit = "Con", MinimumStock = 300, Description = "Tụ hóa dập xung nguồn biến tần" },
                    new() { ProductCode = "IND-SMD-10UH", ProductName = "Cuộn cảm dán vỏ kín Shielded 10uH 4A", CategoryId = catList[4].Id, SupplierId = supList[4].Id, Unit = "Con", MinimumStock = 200, Description = "Cuộn cảm lọc nguồn cho bộ hạ áp Switching" },
                    new() { ProductCode = "IC-OPAMP-LM358", ProductName = "IC Khuếch đại thuật toán kép LM358DR SOP-8", CategoryId = catList[4].Id, SupplierId = supList[3].Id, Unit = "Con", MinimumStock = 250, Description = "Khuếch đại tín hiệu cảm biến độ nhạy cao" }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // 7. Seed Stock Receipts, Inventory, and Stock Transactions (Khớp 100% nghiệp vụ)
            if (!context.StockReceipts.Any())
            {
                var prods = await context.Products.OrderBy(p => p.Id).ToListAsync();
                var locs = await context.WarehouseLocations.OrderBy(l => l.Id).ToListAsync();
                var sups = await context.Suppliers.OrderBy(s => s.Id).ToListAsync();

                // Tạo Phiếu nhập 1 (Đã xác nhận)
                var receipt1 = new StockReceipt
                {
                    ReceiptCode = "PN-20261101-0001",
                    ReceiptDate = DateTime.UtcNow.AddDays(-10),
                    SupplierId = sups[0].Id,
                    CreatedById = adminUser.Id,
                    Status = ReceiptStatus.Confirmed,
                    Note = "Nhập lô CPU Intel đợt đầu tháng 11 phục vụ dây chuyền đóng gói",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    ConfirmedAt = DateTime.UtcNow.AddDays(-10).AddHours(2)
                };

                receipt1.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[0].Id, LocationId = locs[0].Id, Quantity = 50 });
                receipt1.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[1].Id, LocationId = locs[0].Id, Quantity = 60 });

                // Tạo Phiếu nhập 2 (Đã xác nhận)
                var receipt2 = new StockReceipt
                {
                    ReceiptCode = "PN-20261105-0002",
                    ReceiptDate = DateTime.UtcNow.AddDays(-6),
                    SupplierId = sups[1].Id,
                    CreatedById = staffUser.Id,
                    Status = ReceiptStatus.Confirmed,
                    Note = "Nhập module RAM Samsung và chip nhớ NAND",
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    ConfirmedAt = DateTime.UtcNow.AddDays(-6).AddHours(1)
                };

                receipt2.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[4].Id, LocationId = locs[1].Id, Quantity = 100 });
                receipt2.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[5].Id, LocationId = locs[1].Id, Quantity = 50 });
                receipt2.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[6].Id, LocationId = locs[4].Id, Quantity = 80 });

                // Tạo Phiếu nhập 3 (Đã xác nhận - Linh kiện thụ động)
                var receipt3 = new StockReceipt
                {
                    ReceiptCode = "PN-20261108-0003",
                    ReceiptDate = DateTime.UtcNow.AddDays(-3),
                    SupplierId = sups[4].Id,
                    CreatedById = adminUser.Id,
                    Status = ReceiptStatus.Confirmed,
                    Note = "Nhập cuộn tụ điện MLCC và tụ hóa nhôm lọc nguồn",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    ConfirmedAt = DateTime.UtcNow.AddDays(-3).AddHours(3)
                };

                receipt3.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[16].Id, LocationId = locs[5].Id, Quantity = 25 });
                receipt3.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[17].Id, LocationId = locs[5].Id, Quantity = 500 });
                receipt3.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[18].Id, LocationId = locs[5].Id, Quantity = 400 });

                // Tạo Phiếu nhập 4 (Trạng thái Draft để kiểm thử)
                var receipt4 = new StockReceipt
                {
                    ReceiptCode = "PN-20261114-0004",
                    ReceiptDate = DateTime.UtcNow,
                    SupplierId = sups[2].Id,
                    CreatedById = staffUser.Id,
                    Status = ReceiptStatus.Draft,
                    Note = "Phiếu nhập nháp đang chờ kiểm đếm thùng hàng thực tế",
                    CreatedAt = DateTime.UtcNow
                };

                receipt4.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[8].Id, LocationId = locs[2].Id, Quantity = 30 });
                receipt4.ReceiptDetails.Add(new StockReceiptDetail { ProductId = prods[10].Id, LocationId = locs[2].Id, Quantity = 150 });

                await context.StockReceipts.AddRangeAsync(receipt1, receipt2, receipt3, receipt4);
                await context.SaveChangesAsync();

                // Cập nhật tồn kho và tạo StockTransactions cho các phiếu Confirmed
                var confirmedReceipts = new[] { receipt1, receipt2, receipt3 };
                foreach (var r in confirmedReceipts)
                {
                    foreach (var d in r.ReceiptDetails)
                    {
                        var inv = await context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == d.ProductId && i.LocationId == d.LocationId);

                        if (inv == null)
                        {
                            inv = new Inventory
                            {
                                ProductId = d.ProductId,
                                LocationId = d.LocationId,
                                Quantity = d.Quantity,
                                UpdatedAt = r.ConfirmedAt ?? DateTime.UtcNow
                            };
                            await context.Inventories.AddAsync(inv);
                        }
                        else
                        {
                            inv.Quantity += d.Quantity;
                            inv.UpdatedAt = r.ConfirmedAt ?? DateTime.UtcNow;
                        }

                        // Ghi nhận transaction
                        var tx = new StockTransaction
                        {
                            ProductId = d.ProductId,
                            LocationId = d.LocationId,
                            TransactionType = TransactionType.In,
                            Quantity = d.Quantity,
                            ReferenceCode = r.ReceiptCode,
                            PerformedById = r.CreatedById,
                            CreatedAt = r.ConfirmedAt ?? DateTime.UtcNow,
                            Note = $"Nhập kho theo phiếu {r.ReceiptCode}"
                        };
                        await context.StockTransactions.AddAsync(tx);
                    }
                }
                await context.SaveChangesAsync();

                // 8. Seed Stock Issue (Phiếu xuất đã xác nhận)
                var issue1 = new StockIssue
                {
                    IssueCode = "PX-20261110-0001",
                    IssueDate = DateTime.UtcNow.AddDays(-2),
                    CreatedById = adminUser.Id,
                    Status = IssueStatus.Confirmed,
                    Note = "Cấp phát CPU và RAM cho dây chuyền SMT Line 1 lắp ráp Mainboard",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ConfirmedAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
                };

                issue1.IssueDetails.Add(new StockIssueDetail { ProductId = prods[0].Id, LocationId = locs[0].Id, Quantity = 15 });
                issue1.IssueDetails.Add(new StockIssueDetail { ProductId = prods[4].Id, LocationId = locs[1].Id, Quantity = 25 });

                // Phiếu xuất nháp (Draft)
                var issue2 = new StockIssue
                {
                    IssueCode = "PX-20261114-0002",
                    IssueDate = DateTime.UtcNow,
                    CreatedById = staffUser.Id,
                    Status = IssueStatus.Draft,
                    Note = "Yêu cầu cấp phát linh kiện tụ điện cho tổ bảo trì",
                    CreatedAt = DateTime.UtcNow
                };
                issue2.IssueDetails.Add(new StockIssueDetail { ProductId = prods[17].Id, LocationId = locs[5].Id, Quantity = 50 });

                await context.StockIssues.AddRangeAsync(issue1, issue2);
                await context.SaveChangesAsync();

                // Cập nhật kho và transaction cho issue1
                foreach (var d in issue1.IssueDetails)
                {
                    var inv = await context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == d.ProductId && i.LocationId == d.LocationId);

                    if (inv != null)
                    {
                        inv.Quantity -= d.Quantity;
                        inv.UpdatedAt = issue1.ConfirmedAt ?? DateTime.UtcNow;
                    }

                    var tx = new StockTransaction
                    {
                        ProductId = d.ProductId,
                        LocationId = d.LocationId,
                        TransactionType = TransactionType.Out,
                        Quantity = d.Quantity,
                        ReferenceCode = issue1.IssueCode,
                        PerformedById = issue1.CreatedById,
                        CreatedAt = issue1.ConfirmedAt ?? DateTime.UtcNow,
                        Note = $"Xuất kho theo phiếu {issue1.IssueCode}"
                    };
                    await context.StockTransactions.AddAsync(tx);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
