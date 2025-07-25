using System;
using System.Collections.Generic;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Fakeorder> Fakeorders { get; set; }

    public virtual DbSet<Fakeorder1> Fakeorder1s { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Numbersheet> Numbersheets { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductsImg> ProductsImgs { get; set; }

    public virtual DbSet<TblAddcart> TblAddcarts { get; set; }

    public virtual DbSet<TblCategory> TblCategories { get; set; }

    public virtual DbSet<TblDeliveryuser> TblDeliveryusers { get; set; }

    public virtual DbSet<TblEmployee> TblEmployees { get; set; }

    public virtual DbSet<TblFollow> TblFollows { get; set; }

    public virtual DbSet<TblGroupby> TblGroupbies { get; set; }

    public virtual DbSet<TblGroupjoin> TblGroupjoins { get; set; }

    public virtual DbSet<TblImage> TblImages { get; set; }

    public virtual DbSet<TblMenu> TblMenus { get; set; }

    public virtual DbSet<TblMenu1> TblMenu1s { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    public virtual DbSet<TblOrder> TblOrders { get; set; }

    public virtual DbSet<TblOrdernow> TblOrdernows { get; set; }

    public virtual DbSet<TblProduct> TblProducts { get; set; }

    public virtual DbSet<TblProductdetail> TblProductdetails { get; set; }

    public virtual DbSet<TblRating> TblRatings { get; set; }

    public virtual DbSet<TblShiping> TblShipings { get; set; }

    public virtual DbSet<TblSilder> TblSilders { get; set; }

    public virtual DbSet<TblSubcategory> TblSubcategories { get; set; }
    public virtual DbSet<TblChildSubcategory> TblChildSubcategories { get; set; }

    public virtual DbSet<TblUser> TblUsers { get; set; }

    public virtual DbSet<TblWallet> TblWallets { get; set; }

    public virtual DbSet<TblWishlist> TblWishlists { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<ViewBlog> ViewBlogs { get; set; }

    public virtual DbSet<VwCart> VwCarts { get; set; }

    public virtual DbSet<VwCartold> VwCartolds { get; set; }

    public virtual DbSet<VwCartold1> VwCartold1s { get; set; }

    public virtual DbSet<VwCartold2> VwCartold2s { get; set; }

    public virtual DbSet<VwFollow> VwFollows { get; set; }

    public virtual DbSet<VwGroup> VwGroups { get; set; }

    public virtual DbSet<VwGroup1> VwGroup1s { get; set; }

    public virtual DbSet<VwGrouprefercode> VwGrouprefercodes { get; set; }

    public virtual DbSet<VwImage> VwImages { get; set; }

    public virtual DbSet<VwOrder> VwOrders { get; set; }

    public virtual DbSet<VwProduct> VwProducts { get; set; }
    public virtual DbSet<VwTopProducts> VwTopProducts { get; set; }

    public virtual DbSet<VwUserrefercode> VwUserrefercodes { get; set; }

    public virtual DbSet<VwWhishlist> VwWhishlists { get; set; }
    public virtual DbSet<TblUserReferral> TblUserReferrals { get; set; }
    public virtual DbSet<TblPromocode> TblPromocodes { get; set; }
    public virtual DbSet<TblPromocodeUsage> TblPromocodeUsages { get; set; }
    public virtual DbSet<TblPromoProduct> TblPromoProducts { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=103.120.176.21;Integrated Security=False; DATABASE=tridente_ecomme1;  Password=ecomme1@#$; User ID=tridente_ecomme1; Connect Timeout=15;Encrypt=False;Packet Size=4096");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tridente_ecomme1");

        modelBuilder.Entity<Address>(entity =>
        {
            entity
                .ToTable("address", "dbo");

            entity.HasKey(e => e.AdId); // <-- Set Primary Key

            entity.Property(e => e.AdId).HasColumnName("ad_id");

            entity.Property(e => e.AdAddress1)
                .IsUnicode(false)
                .HasColumnName("ad_address1");

            entity.Property(e => e.AdAddress2)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ad_address2");

            entity.Property(e => e.AdCity)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ad_city");

            entity.Property(e => e.AdLandmark)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ad_landmark");

            entity.Property(e => e.AdPincode)
                .HasColumnName("ad_pincode");

            entity.Property(e => e.IsPrimary)
                .HasDefaultValueSql("('0')")
                .HasColumnName("is_primary");

            entity.Property(e => e.UId)
                .HasColumnName("u_id");

            entity.Property(e => e.AdContact)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("ad_contact");

            entity.Property(e => e.AdName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ad_name");
        });


        modelBuilder.Entity<Cart>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("cart", "dbo");

            entity.Property(e => e.AtDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("at_date");
            entity.Property(e => e.CId).HasColumnName("c_id");
            entity.Property(e => e.CQun).HasColumnName("c_qun");
            entity.Property(e => e.GrpId).HasColumnName("grp_id");
            entity.Property(e => e.HasOrdered)
                .HasDefaultValueSql("('0')")
                .HasColumnName("hasOrdered");
            entity.Property(e => e.PId).HasColumnName("p_id");
            entity.Property(e => e.SaveLeter)
                .HasDefaultValueSql("('0')")
                .HasColumnName("save_leter");
            entity.Property(e => e.UId).HasColumnName("u_id");
        });

        modelBuilder.Entity<Fakeorder>(entity =>
        {
            entity.HasKey(e => e.Fid).HasName("PK__fakeorde__C1D1314A9CE0EC9C");

            entity.ToTable("fakeorder", "dbo");

            entity.Property(e => e.Cartid).HasColumnName("cartid");
            entity.Property(e => e.Orderdatetime)
                .HasColumnType("datetime")
                .HasColumnName("orderdatetime");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<Fakeorder1>(entity =>
        {
            entity.HasKey(e => e.Fid).HasName("PK__fakeorde__C1D1314AF7BABE42");

            entity.ToTable("fakeorder1", "dbo");

            entity.Property(e => e.Cartid).HasColumnName("cartid");
            entity.Property(e => e.Orderdatetime)
                .HasColumnType("datetime")
                .HasColumnName("orderdatetime");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("News", "dbo");

            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Descr).HasColumnName("descr");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Image1).HasColumnName("image1");
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Newseo)
                .IsUnicode(false)
                .HasColumnName("newseo");
        });

        modelBuilder.Entity<Numbersheet>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("numbersheet", "dbo");

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Id).HasColumnName("Id ");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("phone ");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("products");

            entity.Property(e => e.AtDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("at_date");
            entity.Property(e => e.BrandName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("brand_name");
            entity.Property(e => e.CsId).HasColumnName("cs_id");
            entity.Property(e => e.IsAvl)
                .HasDefaultValueSql("('1')")
                .HasColumnName("is_avl");
            entity.Property(e => e.MarketPrice).HasColumnName("market_price");
            entity.Property(e => e.Oos)
                .HasDefaultValueSql("('0')")
                .HasColumnName("OOS");
            entity.Property(e => e.PDesc)
                .IsUnicode(false)
                .HasColumnName("p_desc");
            entity.Property(e => e.PId).HasColumnName("p_id");
            entity.Property(e => e.PMeasurement)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("p_measurement");
            entity.Property(e => e.PName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("p_name");
            entity.Property(e => e.PPros)
                .IsUnicode(false)
                .HasColumnName("p_pros");
            entity.Property(e => e.PWeightUnit)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("p_weight_unit");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price");
            entity.Property(e => e.Top)
                .HasDefaultValueSql("('0')")
                .HasColumnName("top");
            entity.Property(e => e.UId).HasColumnName("u_id");
        });

        modelBuilder.Entity<ProductsImg>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("products_imgs");

            entity.Property(e => e.IName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("i_name");
            entity.Property(e => e.PId).HasColumnName("p_id");
            entity.Property(e => e.PiId).HasColumnName("pi_id");
        });

        modelBuilder.Entity<TblAddcart>(entity =>
        {
            entity.ToTable("tbl_addcart", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblCategory>(entity =>
        {
            entity.ToTable("tbl_category", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(200)
                .HasColumnName("categoryName");
            entity.Property(e => e.Image).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblDeliveryuser>(entity =>
        {
            entity.ToTable("tbl_Deliveryuser", "dbo");

            entity.HasIndex(e => e.Email, "UQ__tbl_Deli__A9D1053440214C57").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AadharCardNo).HasMaxLength(20);
            entity.Property(e => e.AccountNo).HasMaxLength(50);
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BusinessLocation).HasMaxLength(255);
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Gst)
                .HasMaxLength(20)
                .HasColumnName("GST");
            entity.Property(e => e.Idproof).HasColumnName("IDProof");
            entity.Property(e => e.Ifsccode)
                .HasMaxLength(20)
                .HasColumnName("IFSCCode");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Pan)
                .HasMaxLength(20)
                .HasColumnName("PAN");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Refid).HasColumnName("refid");
            entity.Property(e => e.Reject)
                .HasDefaultValue(0)
                .HasColumnName("reject");
            entity.Property(e => e.RejectRemark).HasColumnName("rejectRemark");
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserType).HasColumnName("userType");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblEmployee>(entity =>
        {
            entity.ToTable("tbl_Employee", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AssetId).HasColumnName("AssetID");
            entity.Property(e => e.DepId).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.EmpCode).HasMaxLength(50);
            entity.Property(e => e.FrechiseMgrId)
                .HasMaxLength(50)
                .HasColumnName("FrechiseMgrID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Password).HasMaxLength(200);
            entity.Property(e => e.PhoneNo).HasMaxLength(12);
            entity.Property(e => e.RepMgrId).HasColumnName("RepMgrID");
            entity.Property(e => e.Roles).HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(10);
        });

        modelBuilder.Entity<TblFollow>(entity =>
        {
            entity.ToTable("tbl_follow", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValue(false)
                .HasColumnName("status");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Vendorid).HasColumnName("vendorid");
        });

        modelBuilder.Entity<TblGroupby>(entity =>
        {
            entity.ToTable("tbl_Groupby", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Acctt)
                .HasDefaultValue(false)
                .HasColumnName("acctt");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EventSend).HasComputedColumnSql("(dateadd(day,(1),CONVERT([date],[AddedDate])))", true);
            entity.Property(e => e.EventSend1)
                .HasComputedColumnSql("(dateadd(day,(1),[AddedDate]))", true)
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Sipid).HasColumnName("sipid");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblGroupjoin>(entity =>
        {
            entity.ToTable("tbl_Groupjoin", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Acctt)
                .HasDefaultValue(false)
                .HasColumnName("acctt");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Sipid).HasColumnName("sipid");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblImage>(entity =>
        {
            entity.ToTable("tbl_image", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Image)
                .HasMaxLength(200)
                .HasColumnName("image");
            entity.Property(e => e.Imagepath).HasColumnName("imagepath");
            entity.Property(e => e.Imagepath1).HasColumnName("imagepath1");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblMenu>(entity =>
        {
            entity.HasKey(e => e.MenuId);

            entity.ToTable("tbl_Menu", "dbo");

            entity.Property(e => e.CreateTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAll).HasDefaultValue(false);
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.MenuName).HasMaxLength(50);
            entity.Property(e => e.ModifyTime)
                .HasColumnType("datetime")
                .HasColumnName("ModifyTIme");
        });

        modelBuilder.Entity<TblMenu1>(entity =>
        {
            entity.HasKey(e => e.MenuId);

            entity.ToTable("tbl_Menu1", "dbo");

            entity.Property(e => e.CreateTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAll).HasDefaultValue(false);
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.MenuName).HasMaxLength(50);
            entity.Property(e => e.ModifyTime)
                .HasColumnType("datetime")
                .HasColumnName("ModifyTIme");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.ToTable("tbl_notification", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Acctt)
                .HasDefaultValue(false)
                .HasColumnName("acctt");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Sipid).HasColumnName("sipid");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<TblOrder>(entity =>
        {
            entity.ToTable("tbl_Order", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Productdetails)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("productdetails");
            entity.Property(e => e.Productid).HasMaxLength(200);
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblOrdernow>(entity =>
        {
            entity.ToTable("tbl_Ordernow", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.TrackId)
        .HasColumnName("TrackId")
        .HasMaxLength(50);
            entity.Property(e => e.Acctt)
                .HasDefaultValue(false)
                .HasColumnName("acctt");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Dassignid).HasDefaultValue(0);
            entity.Property(e => e.DassignidTime).HasColumnType("datetime");
            entity.Property(e => e.Ddeliverredid).HasDefaultValue(0);
            entity.Property(e => e.DdeliverredidTime).HasColumnType("datetime");
            entity.Property(e => e.DeliveryboyAssginid1).HasColumnName("deliveryboyAssginid1");
            entity.Property(e => e.Deliveryprice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("deliveryprice");
            entity.Property(e => e.Duserassginid).HasDefaultValue(0);
            entity.Property(e => e.DuserassginidTime).HasColumnType("datetime");
            entity.Property(e => e.Dvendorpickup).HasDefaultValue(0);
            entity.Property(e => e.DvendorpickupTime).HasColumnType("datetime");
            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.ShipOrderid).HasDefaultValue(0);
            entity.Property(e => e.ShipOrderidTime).HasColumnType("datetime");
            entity.Property(e => e.Sipid).HasColumnName("sipid");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Userratid)
                .HasDefaultValue(0)
                .HasColumnName("userratid");
        });

        modelBuilder.Entity<TblProduct>(entity =>
        {
            entity.ToTable("tbl_product", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.childcategoryid).HasColumnName("ChildCategoryId");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblProductdetail>(entity =>
        {
            entity.ToTable("tbl_productdetails", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Colorcode)
                .HasMaxLength(200)
                .HasColumnName("colorcode");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Gst)
                .HasMaxLength(200)
                .HasColumnName("gst");
            entity.Property(e => e.Hsncode)
                .HasMaxLength(200)
                .HasColumnName("hsncode");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.Price).HasMaxLength(200);
            entity.Property(e => e.StockQty).HasMaxLength(200);
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Wtype)
                .HasMaxLength(200)
                .HasColumnName("wtype");
            entity.Property(e => e.Wweight)
                .HasMaxLength(200)
                .HasColumnName("wweight");
        });

        modelBuilder.Entity<TblRating>(entity =>
        {
            entity.ToTable("tbl_rating", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Acctt)
                .HasDefaultValue(false)
                .HasColumnName("acctt");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Descr).HasColumnName("descr");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Ratingid).HasColumnName("ratingid");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblShiping>(entity =>
        {
            entity.ToTable("tbl_shiping", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblSilder>(entity =>
        {
            entity.ToTable("tbl_silder", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblSubcategory>(entity =>
        {
            entity.ToTable("tbl_Subcategory", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Image).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.SubcategoryName).HasMaxLength(200);
        });
        
        modelBuilder.Entity<TblChildSubcategory>(entity =>
        {
            entity.ToTable("tbl_ChildSubcategory", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Subcategoryid).HasColumnName("subcategoryid");
            entity.Property(e => e.Image).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.ChildcategoryName).HasMaxLength(200);
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.ToTable("tbl_user", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AadharCardNo).HasMaxLength(20);
            entity.Property(e => e.AccountNo).HasMaxLength(50);
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BusinessLocation).HasMaxLength(255);
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Gender).HasMaxLength(200);
            entity.Property(e => e.Gst)
                .HasMaxLength(20)
                .HasColumnName("GST");
            entity.Property(e => e.Idproof).HasColumnName("IDProof");
            entity.Property(e => e.Ifsccode)
                .HasMaxLength(20)
                .HasColumnName("IFSCCode");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Pan)
                .HasMaxLength(20)
                .HasColumnName("PAN");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Refid).HasColumnName("refid");
            entity.Property(e => e.Reject)
                .HasDefaultValue(0)
                .HasColumnName("reject");
            entity.Property(e => e.RejectRemark).HasColumnName("rejectRemark");
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserType).HasColumnName("userType");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefferalCode)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblWallet>(entity =>
        {
            entity.ToTable("tbl_wallet", "dbo");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Acctt)
                .HasDefaultValue(false)
                .HasColumnName("acctt");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ReferAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("referamount");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TblUserReferral>(entity =>
        {
            entity.ToTable("tbl_UserReferral", "dbo");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.InviterUserId)
                .HasColumnName("InviterUserId")
                .IsRequired();

            entity.Property(e => e.NewUserId)
                .HasColumnName("NewUserId")
                .IsRequired();

            entity.Property(e => e.UsedReferralCode)
                .HasColumnName("UsedReferralCode")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.RewardAmount)
                .HasColumnName("RewardAmount")
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0);

            entity.Property(e => e.IsRewarded)
                .HasColumnName("IsRewarded")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.ModifiedAt)
                .HasColumnName("ModifiedAt")
                .HasColumnType("datetime");

            entity.Property(e => e.IsDeleted)
                .HasColumnName("IsDeleted")
                .HasDefaultValue(false);

            entity.Property(e => e.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true);
        });



        modelBuilder.Entity<TblWishlist>(entity =>
        {
            entity.ToTable("tbl_wishlist", "dbo");

            entity.HasKey(e => e.Id); // ✅ Now EF can track it

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.ToTable("users");

            entity.Property(e => e.AtDate)
                .HasMaxLength(30)
                .HasColumnName("at_date");
            entity.Property(e => e.Dob)
                .HasMaxLength(30)
                .HasColumnName("dob");
            entity.Property(e => e.Ftoken).HasColumnName("ftoken");
            entity.Property(e => e.FullName)
                .HasMaxLength(30)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasMaxLength(30)
                .HasColumnName("is_active");
            entity.Property(e => e.IsAdmin)
                .HasDefaultValueSql("('0')")
                .HasColumnName("is_admin");
            entity.Property(e => e.LastOtp)
                .HasMaxLength(30)
                .HasColumnName("last_otp");
            entity.Property(e => e.LoggingCount)
                .HasMaxLength(30)
                .HasColumnName("logging_count");
            entity.Property(e => e.OtpTime)
                .HasColumnName("otp_time");
            entity.Property(e => e.Password)
                .HasMaxLength(20)
                .HasColumnName("password");
            entity.Property(e => e.Stoken)
                .HasMaxLength(10)
                .HasColumnName("stoken");
            entity.Property(e => e.Token)
                .HasMaxLength(50)
                .HasColumnName("token");
            entity.Property(e => e.UserContact)
                .HasMaxLength(15)
                .HasColumnName("user_contact");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(30)
                .HasColumnName("user_email");
            entity.Property(e => e.UserGender)
                .HasMaxLength(10)
                .HasColumnName("user_gender");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserType)
                .HasMaxLength(10)
                .HasDefaultValue("user")
                .HasColumnName("user_type");
            entity.Property(e => e.Utoken)
                .HasMaxLength(10)
                .HasColumnName("utoken");
        });

        modelBuilder.Entity<ViewBlog>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("View_BLOG", "dbo");

            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Descr).HasColumnName("descr");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Image1).HasColumnName("image1");
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.SeoFriendlyUrl)
                .IsUnicode(false)
                .HasColumnName("SEO_Friendly_URL");
        });

        modelBuilder.Entity<VwCart>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_cart", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.Aid).HasColumnName("aid");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price)
                .HasMaxLength(200)
                .HasColumnName("price");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.Qtyprice)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("qtyprice");
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GroupCode)
                .HasMaxLength(100)
                .HasColumnName("groupcode");
        });

        modelBuilder.Entity<VwCartold>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_cartold", "dbo");

            entity.Property(e => e.AdAddress1)
                .IsUnicode(false)
                .HasColumnName("ad_address1");
            entity.Property(e => e.AdAddress2)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ad_address2");
            entity.Property(e => e.AdName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ad_name");
            entity.Property(e => e.AdPincode).HasColumnName("ad_pincode");
            entity.Property(e => e.Aid).HasColumnName("aid");
            entity.Property(e => e.CQun).HasColumnName("c_qun");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.GrpId).HasColumnName("grp_id");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Netprice).HasColumnName("netprice");
            entity.Property(e => e.Orderdate)
                .HasColumnType("datetime")
                .HasColumnName("orderdate");
            entity.Property(e => e.Orderdate1)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("orderdate1");
            entity.Property(e => e.PName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("p_name");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
        });

        modelBuilder.Entity<VwCartold1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_cartold1", "dbo");

            entity.Property(e => e.AdAddress1)
                .IsUnicode(false)
                .HasColumnName("ad_address1");
            entity.Property(e => e.AdAddress2)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ad_address2");
            entity.Property(e => e.AdContact)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("ad_contact");
            entity.Property(e => e.AdName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ad_name");
            entity.Property(e => e.AdPincode).HasColumnName("ad_pincode");
            entity.Property(e => e.Aid).HasColumnName("aid");
            entity.Property(e => e.CQun).HasColumnName("c_qun");
            entity.Property(e => e.CsId).HasColumnName("cs_id");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.GrpId).HasColumnName("grp_id");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Netprice).HasColumnName("netprice");
            entity.Property(e => e.Orderdate)
                .HasColumnType("datetime")
                .HasColumnName("orderdate");
            entity.Property(e => e.Orderdate1)
                .HasMaxLength(4000)
                .HasColumnName("orderdate1");
            entity.Property(e => e.PName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("p_name");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Qtyprice)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("qtyprice");
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
        });

        modelBuilder.Entity<VwCartold2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_cartold2", "dbo");

            entity.Property(e => e.AdAddress1)
                .IsUnicode(false)
                .HasColumnName("ad_address1");
            entity.Property(e => e.AdAddress2)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("ad_address2");
            entity.Property(e => e.AdContact)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("ad_contact");
            entity.Property(e => e.AdName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ad_name");
            entity.Property(e => e.AdPincode).HasColumnName("ad_pincode");
            entity.Property(e => e.Aid).HasColumnName("aid");
            entity.Property(e => e.CQun).HasColumnName("c_qun");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.GrpId).HasColumnName("grp_id");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Netprice).HasColumnName("netprice");
            entity.Property(e => e.Orderdate)
                .HasColumnType("datetime")
                .HasColumnName("orderdate");
            entity.Property(e => e.Orderdate1)
                .HasMaxLength(4000)
                .HasColumnName("orderdate1");
            entity.Property(e => e.PName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("p_name");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Qtyprice)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("qtyprice");
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");
        });

        modelBuilder.Entity<VwFollow>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_follow", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.Companyname).HasMaxLength(100);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image1).HasColumnName("image1");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Vendorid).HasColumnName("vendorid");
        });

        modelBuilder.Entity<VwGroup>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_group", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(200)
                .HasColumnName("categoryName");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.EventSend1).HasColumnType("datetime");
            entity.Property(e => e.Gid).HasColumnName("gid");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Image1).HasColumnName("image1");
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.NetQty).HasColumnName("net_qty");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.Orderdate)
                .HasColumnType("datetime")
                .HasColumnName("orderdate");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price)
                .HasMaxLength(200)
                .HasColumnName("price");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.Qtyprice)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("qtyprice");
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.SubcategoryName).HasMaxLength(200);
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
                entity.Property(e => e.GroupCode)
                .HasMaxLength(100)
                .HasColumnName("groupcode");
        });

        modelBuilder.Entity<VwGroup1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_group1", "dbo");

            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.EventSend1).HasColumnType("datetime");
            entity.Property(e => e.Eventsdate1)
                .HasMaxLength(4000)
                .HasColumnName("eventsdate1");
            entity.Property(e => e.Gid).HasColumnName("gid");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Image1).HasColumnName("image1");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.RemainingQty).HasColumnName("remaining_qty");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwGrouprefercode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Grouprefercode", "dbo");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Refercode)
                .HasMaxLength(9)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwImage>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_image", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.Image)
                .HasMaxLength(200)
                .HasColumnName("image");
            entity.Property(e => e.Imagepath)
                .HasMaxLength(258)
                .HasColumnName("imagepath");
            entity.Property(e => e.Imagepath1)
                .HasMaxLength(260)
                .HasColumnName("imagepath1");
            entity.Property(e => e.Imagepath2)
                .HasMaxLength(334)
                .HasColumnName("imagepath2");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwOrder>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_order", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(200)
                .HasColumnName("categoryName");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Ddname)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DDNAME");
            entity.Property(e => e.DeliveryboyAssginid1).HasColumnName("deliveryboyAssginid1");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Dpname)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPNAME");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.GroupCount).HasColumnName("group_count");
            entity.Property(e => e.GroupQty).HasColumnName("group_qty");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.Orderdate)
                .HasColumnType("datetime")
                .HasColumnName("orderdate");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Phone2)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("phone2");
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price)
                .HasMaxLength(200)
                .HasColumnName("price");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.Qtyprice)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("qtyprice");
            entity.Property(e => e.SellerPostalcode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sellerPostalcode");
            entity.Property(e => e.Sellername)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("sellername");
            entity.Property(e => e.Sellerphone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sellerphone");
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.SubcategoryName).HasMaxLength(200);
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Userratid).HasColumnName("userratid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwProduct>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Product", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.Unit).HasColumnType("unit");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(200)
                .HasColumnName("categoryName");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.ChildCategoryId).HasColumnName("ChildCategoryId");
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Gst)
                .HasMaxLength(200)
                .HasColumnName("gst");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.SpecialTags).HasColumnName("specialtags");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Price)
                .HasMaxLength(200)
                .HasColumnName("price");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductNameShort).HasMaxLength(203);
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.SubcategoryName).HasMaxLength(200);
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("vendorName");
            entity.Property(e => e.Wtype)
                .HasMaxLength(200)
                .HasColumnName("wtype");
            entity.Property(e => e.Wweight)
                .HasMaxLength(200)
                .HasColumnName("wweight");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(200)
                .HasColumnName("groupcode");
        });

        modelBuilder.Entity<VwTopProducts>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TopProducts", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.Unit).HasColumnType("unit");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(200)
                .HasColumnName("categoryName");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.ChildCategoryId).HasColumnName("ChildCategoryId");
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Gst)
                .HasMaxLength(200)
                .HasColumnName("gst");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.SpecialTags).HasColumnName("specialtags");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Price)
                .HasMaxLength(200)
                .HasColumnName("price");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductNameShort).HasMaxLength(203);
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.SubcategoryName).HasMaxLength(200);
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("vendorName");
            entity.Property(e => e.Wtype)
                .HasMaxLength(200)
                .HasColumnName("wtype");
            entity.Property(e => e.Wweight)
                .HasMaxLength(200)
                .HasColumnName("wweight");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(200)
                .HasColumnName("groupcode");
            entity.Property(e => e.CastedPrice)
                .HasColumnName("CastedPrice")
                .HasColumnType("decimal(10,2)");

        });

        modelBuilder.Entity<TblPromocode>(entity =>
        {
            entity.ToTable("tbl_promocode", "dbo");

            entity.HasKey(e => e.PromoId);

            entity.Property(e => e.PromoId).HasColumnName("promo_id");

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("code");

            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");

            entity.Property(e => e.DiscountType)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("discount_type");

            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("discount_value");

            entity.Property(e => e.MinOrderValue)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("min_order_value");

            entity.Property(e => e.MaxDiscount)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("max_discount");

            entity.Property(e => e.UsageLimit)
                .HasColumnName("usage_limit");

            entity.Property(e => e.PerUserLimit)
                .HasColumnName("per_user_limit");

            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");

            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");

            entity.Property(e => e.AddedDate)
                .HasColumnType("datetime")
                .HasColumnName("AddedDate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("ModifiedDate");

            entity.Property(e => e.RewardType)
                .HasMaxLength(30)
                .HasColumnName("reward_type")
                .HasDefaultValue("PROMO_CODE");

            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");

            entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted");

            entity.Property(e => e.IsActive).HasColumnName("IsActive");
        });

        modelBuilder.Entity<TblPromocodeUsage>(entity =>
        {
            entity.ToTable("tbl_promocode_usage","dbo");

            entity.HasKey(e => e.UsageId);

            entity.Property(e => e.UsageId).HasColumnName("usage_id");

            entity.Property(e => e.PromoId)
                .IsRequired()
                .HasColumnName("promo_id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.OrderId)
                .HasColumnName("OrderId");

            entity.Property(e => e.UsedAt)
                .HasColumnType("datetime")
                .HasColumnName("used_at");
            entity.Property(e => e.ScratchRevealed)
                .HasColumnName("scratch_revealed");
        });

        modelBuilder.Entity<TblPromoProduct>(entity =>
        {
            entity.ToTable("tbl_promocode_products", "dbo");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PromoId)
                .HasColumnName("promo_id")
                .IsRequired();

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id")
                .IsRequired();
        });


        modelBuilder.Entity<VwUserrefercode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Userrefercode", "dbo");

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Refercode)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwWhishlist>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_whishlist", "dbo");

            entity.Property(e => e.AddedDate).HasColumnType("datetime");
            entity.Property(e => e.Aid).HasColumnName("aid");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(100)
                .HasColumnName("companyName");
            entity.Property(e => e.CategoryNAme)
                .HasMaxLength(100)
                .HasColumnName("categoryName");
            entity.Property(e => e.SubcategoryName)
                .HasMaxLength(100)
                .HasColumnName("SubcategoryName");
            entity.Property(e => e.Cuserid).HasColumnName("cuserid");
            entity.Property(e => e.Discountprice)
                .HasMaxLength(200)
                .HasColumnName("discountprice");
            entity.Property(e => e.Gprice)
                .HasMaxLength(200)
                .HasColumnName("gprice");
            entity.Property(e => e.Gqty)
                .HasMaxLength(200)
                .HasColumnName("gqty");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(200);
            entity.Property(e => e.Keywords).HasColumnName("keywords");
            entity.Property(e => e.Longdesc).HasColumnName("longdesc");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Netprice)
                .HasMaxLength(200)
                .HasColumnName("netprice");
            entity.Property(e => e.PPros).HasColumnName("p_pros");
            entity.Property(e => e.Pdid).HasColumnName("pdid");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Pid).HasColumnName("pid");
            entity.Property(e => e.Price)
                .HasMaxLength(200)
                .HasColumnName("price");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Shopid).HasColumnName("shopid");
            entity.Property(e => e.Shortdesc).HasColumnName("shortdesc");
            entity.Property(e => e.Totalprice)
                .HasMaxLength(200)
                .HasColumnName("totalprice");
            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.VendorName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
