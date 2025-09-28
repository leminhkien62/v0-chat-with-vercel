# WMS (Warehouse Management System)

## Hệ thống quản lý kho theo vị trí với ASP.NET MVC 5

### Yêu cầu hệ thống

- **Windows Server 2016+** hoặc Windows 10/11
- **IIS 10+** với ASP.NET 4.8
- **SQL Server 2016+** (Express, Standard, hoặc Enterprise)
- **.NET Framework 4.8**
- **Visual Studio 2019+** (để phát triển)

### Cài đặt và triển khai

#### Bước 1: Chuẩn bị SQL Server

1. **Cài đặt SQL Server 2016+**
   \`\`\`bash
   # Tải SQL Server từ Microsoft
   # Cài đặt với Mixed Mode Authentication
   # Tạo user 'wms_user' với password mạnh
   \`\`\`

2. **Chạy scripts tạo database**
   \`\`\`sql
   -- Mở SQL Server Management Studio (SSMS)
   -- Kết nối với SQL Server instance
   -- Chạy lần lượt các file:
   
   -- 1. Tạo database
   Scripts/01_CreateDatabase.sql
   
   -- 2. Tạo bảng và indexes
   Scripts/02_CreateTables.sql
   
   -- 3. Thêm dữ liệu mẫu
   Scripts/03_SeedData.sql
   \`\`\`

#### Bước 2: Cấu hình Connection String

1. **Mở file Web.config**
2. **Cập nhật connection string:**
   \`\`\`xml
   <connectionStrings>
     <add name="DefaultConnection" 
          connectionString="Server=YOUR_SERVER;Database=WMS_Database;User Id=wms_user;Password=YOUR_PASSWORD;MultipleActiveResultSets=true" 
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   \`\`\`

#### Bước 3: Cài đặt NuGet Packages

\`\`\`bash
# Mở Package Manager Console trong Visual Studio
# Chạy lệnh restore packages:

Update-Package -reinstall
\`\`\`

**Danh sách packages chính:**
- EntityFramework 6.4.4
- Microsoft.AspNet.Mvc 5.2.9
- Microsoft.AspNet.Identity.EntityFramework 2.2.3
- Microsoft.AspNet.SignalR 2.4.3
- Bootstrap 5.3.0
- jQuery 3.6.0
- Chart.js 4.4.0
- DataTables 1.13.6
- Select2 4.1.0
- QRCoder 1.4.3
- CsvHelper 30.0.1
- Rotativa 1.7.3

#### Bước 4: Cấu hình IIS

1. **Tạo Application Pool:**
   \`\`\`
   - Tên: WMS_AppPool
   - .NET CLR Version: v4.0
   - Managed Pipeline Mode: Integrated
   - Identity: ApplicationPoolIdentity
   \`\`\`

2. **Tạo Website:**
   \`\`\`
   - Site Name: WMS
   - Physical Path: C:\inetpub\wwwroot\WMS
   - Port: 80 (hoặc 443 cho HTTPS)
   - Application Pool: WMS_AppPool
   \`\`\`

3. **Cấu hình SSL (khuyến nghị):**
   \`\`\`
   - Cài đặt SSL certificate
   - Redirect HTTP to HTTPS
   - Enable HSTS
   \`\`\`

#### Bước 5: Cấu hình ERP Integration (tùy chọn)

\`\`\`xml
<!-- Trong Web.config, thêm app settings: -->
<appSettings>
  <add key="ERP.BaseUrl" value="http://your-erp-server/api/" />
  <add key="ERP.Username" value="wms_integration" />
  <add key="ERP.Password" value="your_password" />
  <add key="ERP.Timeout" value="30000" />
</appSettings>
\`\`\`

### Hướng dẫn sử dụng

#### Đăng nhập lần đầu

1. **Truy cập:** `https://your-server/Account/Login`
2. **Tài khoản mặc định:**
   - Email: `admin@wms.com`
   - Password: `Admin@123`

#### Cấu hình ban đầu

1. **Tạo Departments:**
   - Vào Admin → Departments
   - Thêm các phòng ban của công ty

2. **Tạo Warehouses:**
   - Vào Master Data → Warehouses
   - Thêm các kho cần quản lý

3. **Tạo Locations:**
   - Vào Master Data → Locations
   - Tạo vị trí theo format: WH-ZZ-AA-RR-BB

4. **Tạo Items:**
   - Vào Master Data → Items
   - Thêm danh sách vật tư cần quản lý

5. **Phân quyền:**
   - Vào Admin → User Management
   - Tạo user và gán role phù hợp
   - Cấu hình Department ↔ Warehouse mapping

### Luồng nghiệp vụ chính

#### 1. Receiving (Nhận hàng)

\`\`\`
PO từ ERP → Receiving → Tạo LPN → In QR Code → Putaway
\`\`\`

1. Vào **Operations → Receiving**
2. Chọn PO cần nhận
3. Nhập số lượng thực nhận
4. Chọn vị trí cất hàng
5. Tạo và in LPN

#### 2. Issue (Xuất hàng)

\`\`\`
Request → Pick List (FIFO/FEFO) → Issue → Update Stock
\`\`\`

1. Vào **Operations → Issue**
2. Chọn Request cần xử lý
3. Hệ thống gợi ý Pick List theo FIFO/FEFO
4. Xác nhận xuất hàng

#### 3. PWA Self-Service

\`\`\`
QR Code → Mobile Form → Submit Request → Notification
\`\`\`

1. Quét QR code tại khu vực làm việc
2. Điền form yêu cầu vật tư
3. Submit → thông báo realtime cho kho

### Monitoring và Báo cáo

#### Dashboard Realtime

- **URL:** `/Dashboard`
- **Tính năng:**
  - KPI realtime (stock levels, transactions)
  - Heatmap matrix theo Zone/Aisle
  - Biểu đồ xu hướng 30 ngày
  - Top 10 items xuất nhiều nhất

#### Báo cáo

- **Stock Report:** Tồn kho theo vị trí
- **Transaction Report:** Lịch sử giao dịch
- **LPN Report:** Danh sách LPN
- **Request Report:** Báo cáo yêu cầu lãnh

### Troubleshooting

#### Lỗi thường gặp

1. **Connection timeout:**
   \`\`\`xml
   <!-- Tăng timeout trong Web.config -->
   <httpRuntime executionTimeout="300" maxRequestLength="51200" />
   \`\`\`

2. **SignalR không hoạt động:**
   \`\`\`xml
   <!-- Enable WebSockets trong IIS -->
   <system.webServer>
     <webSocket enabled="true" />
   </system.webServer>
   \`\`\`

3. **In LPN không hoạt động:**
   - Kiểm tra Rotativa configuration
   - Đảm bảo wkhtmltopdf được cài đặt

#### Performance Tuning

1. **Database:**
   \`\`\`sql
   -- Rebuild indexes hàng tuần
   ALTER INDEX ALL ON [Stock] REBUILD;
   ALTER INDEX ALL ON [Transactions] REBUILD;
   
   -- Update statistics
   UPDATE STATISTICS [Stock];
   UPDATE STATISTICS [Transactions];
   \`\`\`

2. **IIS:**
   \`\`\`xml
   <!-- Enable compression -->
   <system.webServer>
     <httpCompression>
       <dynamicTypes>
         <add mimeType="application/json" enabled="true" />
       </dynamicTypes>
     </httpCompression>
   </system.webServer>
   \`\`\`

### Backup và Maintenance

#### Daily Backup Script

\`\`\`sql
-- Tạo job backup hàng ngày
BACKUP DATABASE [WMS_Database] 
TO DISK = 'C:\Backup\WMS_Database_' + FORMAT(GETDATE(), 'yyyyMMdd') + '.bak'
WITH COMPRESSION, CHECKSUM;

-- Cleanup logs cũ (>180 ngày)
DELETE FROM [SystemLogs] WHERE [Timestamp] < DATEADD(day, -180, GETDATE());
\`\`\`

### Support và Liên hệ

- **Documentation:** Tham khảo file specification gốc
- **Issues:** Ghi log chi tiết trong SystemLogs table
- **Updates:** Sử dụng Entity Framework Migrations

### License

Hệ thống sử dụng các thư viện mã nguồn mở (OSS) miễn phí. Xem file packages.config để biết chi tiết các dependencies.
\`\`\`

```powershell file="Scripts/Deploy.ps1"
# WMS Deployment Script
# PowerShell script để tự động deploy WMS system

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName = "WMS_Database",
    
    [Parameter(Mandatory=$true)]
    [string]$IISPath = "C:\inetpub\wwwroot\WMS",
    
    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = "WMS_AppPool"
)

Write-Host "Starting WMS Deployment..." -ForegroundColor Green

# 1. Create Application Pool
Write-Host "Creating IIS Application Pool..." -ForegroundColor Yellow
Import-Module WebAdministration

if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
    Write-Host "Application Pool $AppPoolName already exists, removing..." -ForegroundColor Yellow
    Remove-WebAppPool -Name $AppPoolName
}

New-WebAppPool -Name $AppPoolName -Force
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# 2. Create Website Directory
Write-Host "Creating website directory..." -ForegroundColor Yellow
if (Test-Path $IISPath) {
    Remove-Item $IISPath -Recurse -Force
}
New-Item -ItemType Directory -Path $IISPath -Force

# 3. Copy Application Files
Write-Host "Copying application files..." -ForegroundColor Yellow
# Assuming the script is run from the project root
Copy-Item -Path ".\*" -Destination $IISPath -Recurse -Exclude @("Scripts", "*.ps1", "README.md", ".git*")

# 4. Create IIS Website
Write-Host "Creating IIS Website..." -ForegroundColor Yellow
if (Get-Website -Name "WMS" -ErrorAction SilentlyContinue) {
    Remove-Website -Name "WMS"
}

New-Website -Name "WMS" -PhysicalPath $IISPath -ApplicationPool $AppPoolName -Port 80

# 5. Set Permissions
Write-Host "Setting permissions..." -ForegroundColor Yellow
$acl = Get-Acl $IISPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IUSR", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path $IISPath -AclObject $acl

# 6. Update Web.config
Write-Host "Updating Web.config..." -ForegroundColor Yellow
$webConfigPath = Join-Path $IISPath "Web.config"
if (Test-Path $webConfigPath) {
    $webConfig = [xml](Get-Content $webConfigPath)
    $connectionString = $webConfig.configuration.connectionStrings.add | Where-Object { $_.name -eq "DefaultConnection" }
    if ($connectionString) {
        $connectionString.connectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=true;MultipleActiveResultSets=true"
    }
    $webConfig.Save($webConfigPath)
}

# 7. Create Database (if not exists)
Write-Host "Checking database..." -ForegroundColor Yellow
$sqlCmd = @"
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    CREATE DATABASE [$DatabaseName]
    PRINT 'Database $DatabaseName created successfully'
END
ELSE
BEGIN
    PRINT 'Database $DatabaseName already exists'
END
"@

try {
    Invoke-Sqlcmd -ServerInstance $ServerName -Query $sqlCmd -ErrorAction Stop
    Write-Host "Database check completed" -ForegroundColor Green
} catch {
    Write-Host "Error checking database: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please run the SQL scripts manually" -ForegroundColor Yellow
}

# 8. Start Application Pool and Website
Write-Host "Starting services..." -ForegroundColor Yellow
Start-WebAppPool -Name $AppPoolName
Start-Website -Name "WMS"

Write-Host "WMS Deployment completed successfully!" -ForegroundColor Green
Write-Host "Website URL: http://localhost" -ForegroundColor Cyan
Write-Host "Default login: admin@wms.com / Admin@123" -ForegroundColor Cyan

# 9. Test website
Write-Host "Testing website..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Write-Host "Website is responding correctly!" -ForegroundColor Green
    }
} catch {
    Write-Host "Warning: Website test failed. Please check manually." -ForegroundColor Yellow
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Deployment script completed!" -ForegroundColor Green
