using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_LopHocTrucTuyen.Filter;
using QL_LopHocTrucTuyen.Models;
using System.IO;

namespace QL_LopHocTrucTuyen.Controllers
{
    [YeuCauDangNhap]
    public class HocVienController : Controller
    {
        DataClasses1DataContext db = new DataClasses1DataContext();

        // TRANG CHU
        public ActionResult TrangChu()
        {
            // Giả sử trạng thái "đã duyệt" có giá trị là 1
            List<KhoaHoc> dskh = db.KhoaHocs.Where(kh => kh.TrangThai=="Đã duyệt").ToList();
            return View(dskh);
        }


        public ActionResult TimKiemKhoaHoc(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("TrangChu");
            }
            var ketQua = db.KhoaHocs.Where(kh => kh.TenKhoaHoc.Contains(query)).ToList();
            return View("TrangChu", ketQua);
        }

        public ActionResult LoaiKhoaHoc()
        {
            List<LoaiKhoaHoc> ds = db.LoaiKhoaHocs.ToList();
            return View(ds);
        }

        public ActionResult LocKhoaHoc(string id)
        {
            List<KhoaHoc> loai = db.KhoaHocs.Where(t => t.MaLoaiKhoaHoc == id).ToList();
            return View("TrangChu",loai);
        }

        // HOC TAP
        public ActionResult HocTap()
        {
            var userId = Session["UserId"];
            if (userId == null)
            {
                return RedirectToAction("DangNhap", "HocVien");
            }
            var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == userId);
            if (hocVien == null)
            {
                TempData["Message"] = "Người dùng không tồn tại trong hệ thống.";
                return RedirectToAction("TrangChu");
            }
            var danhSachKhoaHocDaThanhToan = db.ChiTietThanhToans
            .Where(cttt => cttt.ThanhToan.MaHocVien == hocVien.MaHocVien).Select(cttt => cttt.DangKies.Select(dk => dk.KhoaHoc)) .SelectMany(kh => kh) .Distinct() .ToList();
            var dangky = db.DangKies.Where(dk => dk.ChiTietThanhToan.ThanhToan.MaHocVien == hocVien.MaHocVien).Select(dk => dk.MaDangKy).Distinct().ToList();
            Session["madk"]=dangky;
            return View(danhSachKhoaHocDaThanhToan);
        }

        public ActionResult KhoaHoc(string id)
        {
            // Lấy khóa học theo ID
            var khoaHoc = db.KhoaHocs.FirstOrDefault(kh => kh.MaKhoaHoc == id);
            
            if (khoaHoc == null)
            {
                return RedirectToAction("TrangChu", "HocVien");
            }

            // Lưu vào Session nếu cần
            Session["MaKhoa"] = khoaHoc.MaKhoaHoc;
            return View(khoaHoc);
        }
        public ActionResult BaiGiang(string id)
        {
            var baiGiang = db.BaiGiangs.FirstOrDefault(bg => bg.MaBaiGiang == id);
            if (baiGiang == null)
            {
                return RedirectToAction("KhoaHoc");
            }

            // Lấy thông tin khóa học
            var khoaHoc = db.KhoaHocs.FirstOrDefault(kh => kh.Chuongs.Any(c => c.BaiGiangs.Any(bg => bg.MaBaiGiang == id)));
            
            ViewBag.KhoaHoc = khoaHoc;

            return View(baiGiang);
        }


        public ActionResult XemBaiTap(string id)
        {
            var baiTap = db.BaiTaps.FirstOrDefault(bt => bt.MaBaiTap == id);

            if (baiTap == null)
            {
                return HttpNotFound("Không tìm thấy bài tập.");
            }

            return View(baiTap); // Truyền đối tượng BaiTap sang View
        }

        public ActionResult NopBaiTap(string maBaiTap, string maDangKy)
        {
            //var chiTietBaiTap = db.DangKy_BaiTaps.FirstOrDefault(t => t.MaDangKy == maDangKy && t.MaBaiTap == maBaiTap);
            if (string.IsNullOrEmpty(maBaiTap))
            {
                return RedirectToAction("BaiTap", "HocVien");
            }

            ViewBag.MaBaiTap = maBaiTap;
            return View();
        }

        public ActionResult ChiTietBaiTap(string maBaiTap)
        {
            if (string.IsNullOrEmpty(maBaiTap))
            {
                ViewBag.ThongBao = "Không tìm thấy bài tập.";
                return RedirectToAction("BaiTap", "HocVien");
            }

            var chiTietBaiTap = db.DangKy_BaiTaps.FirstOrDefault(t=>t.MaBaiTap == maBaiTap);
            if (chiTietBaiTap == null)
            {
                ViewBag.ThongBao = "Không tìm thấy thông tin bài tập.";
                return View();
            }
            return View(chiTietBaiTap);
        }

        [HttpPost]
        public ActionResult XuLyNopBaiTap(string maBaiTap, HttpPostedFileBase fileUpload)
        {
            string maHV = Session["UserId"] as string;
            if (string.IsNullOrEmpty(maHV))
            {
                TempData["Error"] = "Bạn chưa đăng nhập.";
                return RedirectToAction("DangNhap", "HocVien");
            }

            if (fileUpload != null && fileUpload.ContentLength > 0)
            {
                try
                {
                    // Lấy MaKhoaHoc từ MaBaiTap
                    var khoaHoc = db.BaiTaps.Join(db.Chuongs, bt => bt.MaChuong, c => c.MaChuong, (bt, c) => new { BaiTap = bt, Chuong = c }).Where(x => x.BaiTap.MaBaiTap == maBaiTap)
                    .Select(x => x.Chuong.MaKhoaHoc).FirstOrDefault();

                    if (khoaHoc == null)
                    {
                        TempData["Error"] = "Không tìm thấy khóa học của bài tập.";
                        return RedirectToAction("NopBaiTap", "HocVien", new { maBaiTap });
                    }

                    // Lấy MaDangKy phù hợp với MaHocVien và MaKhoaHoc
                    var dangKy = db.DangKies.Where(dk => dk.MaKhoaHoc == khoaHoc && dk.ChiTietThanhToan.ThanhToan.MaHocVien == maHV).OrderByDescending(dk => dk.ChiTietThanhToan.ThanhToan.NgayThanhToan) 
                     .FirstOrDefault();

                    if (dangKy == null)
                    {
                        TempData["Error"] = "Không tìm thấy đăng ký phù hợp.";
                        return RedirectToAction("NopBaiTap", "HocVien", new { maBaiTap });
                    }

                    string maDK = dangKy.MaDangKy;

                    var baiTap = db.DangKy_BaiTaps.FirstOrDefault(bt => bt.MaBaiTap == maBaiTap && bt.MaDangKy == maDK);

                    if (baiTap == null)
                    {
                        // Trường hợp thêm mới
                        baiTap = new DangKy_BaiTap
                        {
                            MaBaiTap = maBaiTap,
                            MaDangKy = maDK,
                            NgayNop = DateTime.Now,
                            TrangThai = "Đã nộp",
                            FileUpload = Path.GetFileName(fileUpload.FileName)
                        };
                        db.DangKy_BaiTaps.InsertOnSubmit(baiTap);
                    }
                    else
                    {
                        // Trường hợp cập nhật
                        baiTap.FileUpload = Path.GetFileName(fileUpload.FileName);
                        baiTap.TrangThai = "Đã nộp";
                        baiTap.NgayNop = DateTime.Now;
                    }

                    // Lưu file lên server
                    string path = Path.Combine(Server.MapPath("~/Content/File/DangKy_BaiTap"), baiTap.FileUpload);
                    fileUpload.SaveAs(path);

                    db.SubmitChanges();

                    TempData["Success"] = "Bài tập đã được nộp thành công!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Bạn chưa chọn file!";
            }

            return RedirectToAction("NopBaiTap", "HocVien", new { maBaiTap });
        }

        public string TaoMaBinhLuan()
        {
            string prefix = "BL";  // Tiền tố cho mã thanh toán
            int numericPart = 0;   // Phần số của mã

            // Lấy mã thanh toán cuối cùng từ cơ sở dữ liệu
            var maBinhLuanCuoiCung = db.BinhLuans
                                        .OrderByDescending(tt => tt.MaBinhLuan)
                                        .FirstOrDefault();

            if (maBinhLuanCuoiCung != null)
            {
                // Lấy phần số của mã thanh toán và tăng giá trị
                numericPart = int.Parse(maBinhLuanCuoiCung.MaBinhLuan.Substring(2)) + 1;
            }

            string newMaBinhLuan = prefix + numericPart.ToString("D3");

            // Kiểm tra mã thanh toán mới có bị trùng với mã đã có trong cơ sở dữ liệu không
            while (db.BinhLuans.Any(tt => tt.MaBinhLuan == newMaBinhLuan))
            {
                numericPart++; // Tăng giá trị phần số lên
                newMaBinhLuan = prefix + numericPart.ToString("D3");
            }

            return newMaBinhLuan;  // Trả về mã thanh toán mới
        }

        public ActionResult XemBinhLuan(string mabg)
        {
            List<BinhLuan> lst = db.BinhLuans.Where(t => t.MaBaiGiang.ToString() == mabg).ToList();
            return PartialView(lst);
        }

        public ActionResult ThemBinhLuan(string mabg, FormCollection c)
        {
            var userId = Session["UserId"].ToString();
            BinhLuan a = new BinhLuan();

            a.MaBinhLuan = TaoMaBinhLuan();
            a.MaBaiGiang = mabg;
            a.NoiDung = c["binhluanmoi"];
            a.MaNguoiDung = userId;
            a.NgayTao = DateTime.Now;
            a.MaBinhLuanCha = c["binhluancha"];


            db.BinhLuans.InsertOnSubmit(a);
            db.SubmitChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        // CHI TIET KHOA HOC
        public ActionResult ChiTietKhoaHoc(string id)
        {
            var khoaHoc = db.KhoaHocs.FirstOrDefault(kh => kh.MaKhoaHoc == id);
            if (khoaHoc == null)
            {
                return HttpNotFound();
            }
            var userId = Session["UserId"];
            bool isPaid = false;
            if (userId != null)
            {
                var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == userId);
                if (hocVien != null)
                {
                    isPaid = db.ThanhToans.Any(tt => tt.MaHocVien == hocVien.MaHocVien && db.ChiTietThanhToans.Any(ct => ct.MaThanhToan == tt.MaThanhToan &&
                     ct.DangKies.Any(t => t.MaKhoaHoc == id)));
                }
            }
            ViewBag.IsPaid = isPaid;
            ViewBag.MaKhoaHoc = id;
            return View(khoaHoc);
        }
        
        public string TaoMaDanhGia()
        {
            string prefix = "DG";  // Tiền tố cho mã thanh toán
            int numericPart = 0;   // Phần số của mã

            // Lấy mã thanh toán cuối cùng từ cơ sở dữ liệu
            var maDanhGiaCuoiCung = db.DanhGias
                                        .OrderByDescending(tt => tt.MaDanhGia)
                                        .FirstOrDefault();

            if (maDanhGiaCuoiCung != null)
            {
                // Lấy phần số của mã thanh toán và tăng giá trị
                numericPart = int.Parse(maDanhGiaCuoiCung.MaDanhGia.Substring(2)) + 1;
            }

            string newMaDanhGia = prefix + numericPart.ToString("D3");

            // Kiểm tra mã thanh toán mới có bị trùng với mã đã có trong cơ sở dữ liệu không
            while (db.DanhGias.Any(tt => tt.MaDanhGia == newMaDanhGia))
            {
                numericPart++; // Tăng giá trị phần số lên
                newMaDanhGia = prefix + numericPart.ToString("D3");
            }

            return newMaDanhGia;  // Trả về mã thanh toán mới
        }

        [HttpPost]
        public ActionResult LuuDanhGia(string maKhoaHoc, int rate, string nhanXet)
        {
            var userId = Session["UserId"];
            if (userId == null)
            {
                TempData["warning"] = "Bạn cần đăng nhập để đánh giá khóa học.";
                return RedirectToAction("ChiTietKhoaHoc", new { id = maKhoaHoc });
            }

            var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == userId);
            var tenDangNhap = db.NguoiDungs.FirstOrDefault(nd => nd.MaNguoiDung == userId).TenDangNhap;

            if (hocVien == null || string.IsNullOrEmpty(tenDangNhap))
            {
                TempData["warning"] = "Không tìm thấy thông tin học viên.";
                return RedirectToAction("ChiTietKhoaHoc", new { id = maKhoaHoc });
            }

            var dangKy = (from dk in db.DangKies
                          join tt in db.ThanhToans on hocVien.MaHocVien equals tt.MaHocVien
                          join ct in db.ChiTietThanhToans on tt.MaThanhToan equals ct.MaThanhToan
                          where dk.MaChiTiet == ct.MaChiTiet &&
                                dk.MaKhoaHoc == maKhoaHoc &&
                                dk.TrangThai == "Thanh toán"
                          select dk).FirstOrDefault();

            if (dangKy == null)
            {
                TempData["warning"] = "Bạn chưa thanh toán khóa học này, không thể đánh giá.";
                return RedirectToAction("ChiTietKhoaHoc", new { id = maKhoaHoc });
            }

            try
            {
                var danhGia = db.DanhGias.FirstOrDefault(dg => dg.MaDangKy == dangKy.MaDangKy && dg.MaKhoaHoc == maKhoaHoc);

                if (danhGia == null)
                {
                    danhGia = new DanhGia
                    {
                        MaDanhGia = TaoMaDanhGia(), // Tạo mã duy nhất
                        MaDangKy = dangKy.MaDangKy,
                        MaKhoaHoc = maKhoaHoc,
                        Rate = rate,
                        NhanXet = nhanXet,
                        NgayDanhGia = DateTime.Now
                    };
                    db.DanhGias.InsertOnSubmit(danhGia); // Thêm mới
                }
                else
                {
                    danhGia.Rate = rate;
                    danhGia.NhanXet = nhanXet;
                    danhGia.NgayDanhGia = DateTime.Now; // Cập nhật
                }

                db.SubmitChanges();

                TempData["ToastMessage"] = "Đánh giá của bạn đã được lưu thành công!";
                return RedirectToAction("ChiTietKhoaHoc", new { id = maKhoaHoc });
            }
            catch (Exception ex)
            {
                TempData["warning"] = "Có lỗi xảy ra khi lưu đánh giá: " + ex.Message;
                return RedirectToAction("ChiTietKhoaHoc", new { id = maKhoaHoc });
            }
        }



        [HttpGet]
        public JsonResult LayDanhGiaKhoaHoc(string maKhoaHoc)
        {
            var danhGiaList = db.DanhGias
                .Where(dg => dg.MaKhoaHoc == maKhoaHoc)
                .Select(dg => new
                {
                    UserName = db.DangKies.Where(dk => dk.MaDangKy == dg.MaDangKy).Select(dk => db.ChiTietThanhToans.Where(ct => ct.MaChiTiet == dk.MaChiTiet)
                    .Select(ct => db.ThanhToans.Where(tt => tt.MaThanhToan == ct.MaThanhToan).Select(tt => tt.HocVien.NguoiDung.TenDangNhap).FirstOrDefault())
                     .FirstOrDefault()).FirstOrDefault(),
                    Rate = dg.Rate,
                    NhanXet = dg.NhanXet,
                    NgayDanhGia = dg.NgayDanhGia
                })
                .ToList()
                .Select(dg => new
                {dg.UserName,dg.Rate,dg.NhanXet,NgayDanhGia = dg.NgayDanhGia.HasValue ? dg.NgayDanhGia.Value.ToString("dd/MM/yyyy") : null}).ToList();
            return Json(danhGiaList, JsonRequestBehavior.AllowGet);
        }


        // THONG TIN NGUOI DUNG
        public ActionResult ThongTinNguoiDung()
        {
            var userId = Session["UserId"];
            if (userId != null)
            {
                var nguoiDung = db.NguoiDungs.FirstOrDefault(nd => nd.MaNguoiDung == userId);
                var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == nguoiDung.MaNguoiDung);
                if (nguoiDung != null)
                {
                    ViewBag.TenDangNhap = nguoiDung.TenDangNhap ?? string.Empty;
                    ViewBag.Email = nguoiDung.Email ?? string.Empty;

                    if (hocVien != null)
                    {
                        ViewBag.HoTen = hocVien.HoTen ?? string.Empty;
                        ViewBag.GioiTinh = hocVien.GioiTinh ?? string.Empty;
                        ViewBag.SoDienThoai = hocVien.SoDienThoai ?? string.Empty;
                        ViewBag.DiaChi = hocVien.DiaChi ?? string.Empty;
                        ViewBag.NgaySinh = hocVien.NgaySinh.HasValue ? hocVien.NgaySinh.Value.ToString("dd/MM/yyyy") : "Chưa xác định";
                    }
                    else
                    {
                        ViewBag.HoTen = string.Empty;
                        ViewBag.GioiTinh = string.Empty;
                        ViewBag.SoDienThoai = string.Empty;
                        ViewBag.DiaChi = string.Empty;
                        ViewBag.NgaySinh = "Chưa xác định";
                    }
                    ViewBag.AnhBia = Session["AnhBia"] ?? "~/Content/HocVien/Images/Default_Avatar.png";
                    return View();
                }
            }
            return RedirectToAction("DangNhap", "HocVien");
        }

        [HttpPost]
        public ActionResult UploadAvatar(HttpPostedFileBase avatar)
        {
            if (avatar != null && avatar.ContentLength > 0)
            {
                // Đường dẫn lưu ảnh
                string fileName = Path.GetFileName(avatar.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar/"), fileName);
                Session["AnhBia"] = fileName;
                // Lưu ảnh vào thư mục
                avatar.SaveAs(path);

                // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
                var userId = Session["UserId"];
                var user = db.NguoiDungs.FirstOrDefault(nd => nd.MaNguoiDung == userId);
                if (user != null)
                {
                    user.AnhDaiDien = fileName; // Lưu tên file ảnh vào cơ sở dữ liệu
                    db.SubmitChanges();
                }

                return Json(new { success = true, filename = fileName });
            }
            return Json(new { success = false, message = "Có lỗi xảy ra khi tải lên ảnh." });
        }

        [HttpPost]
        public ActionResult ChinhSua(string email, string hoTen, DateTime? ngaySinh, string gioiTinh, string soDienThoai, string diaChi)
        {
            try
            {
                var userId = Session["UserId"];
                if (userId != null)
                {
                    var nguoiDung = db.NguoiDungs.FirstOrDefault(nd => nd.MaNguoiDung == userId);
                    var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == nguoiDung.MaNguoiDung);

                    if (nguoiDung != null && hocVien != null)
                    {
                        nguoiDung.Email = email ?? nguoiDung.Email;
                        hocVien.HoTen = hoTen ?? hocVien.HoTen;
                        hocVien.NgaySinh = ngaySinh ?? hocVien.NgaySinh;
                        hocVien.GioiTinh = gioiTinh ?? hocVien.GioiTinh;
                        hocVien.SoDienThoai = soDienThoai ?? hocVien.SoDienThoai;
                        hocVien.DiaChi = diaChi ?? hocVien.DiaChi;
                        db.SubmitChanges();
                        return Json(new { success = true, message = "Thông tin đã được cập nhật thành công!" });
                    }
                }
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật thông tin. Người dùng không tồn tại." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Cập nhật thất bại: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ChinhSuaEmail(string email)
        {
            try
            {
                var userId = Session["UserId"];
                if (userId != null)
                {
                    var nguoiDung = db.NguoiDungs.FirstOrDefault(nd => nd.MaNguoiDung == userId);

                    if (nguoiDung != null)
                    {
                        nguoiDung.Email = email;
                        db.SubmitChanges();

                        return Json(new { success = true, message = "Email đã được cập nhật thành công!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Người dùng không tồn tại." });
                    }
                }
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Cập nhật thất bại: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ChinhSuaSoDienThoai(string soDienThoai)
        {
            try
            {
                var userId = Session["UserId"];
                if (userId != null)
                {
                    var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == userId);
                    if (hocVien != null)
                    {
                        hocVien.SoDienThoai = soDienThoai;
                        db.SubmitChanges();
                        return Json(new { success = true, message = "Số điện thoại đã được cập nhật thành công!" });
                    }
                }
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật số điện thoại. Người dùng không tồn tại." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Cập nhật thất bại: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DoiMatKhau(string currentPassword, string newPassword)
        {
            var userId = Session["UserId"];
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đổi mật khẩu." });
            }

            var nguoiDung = db.NguoiDungs.FirstOrDefault(nd => nd.MaNguoiDung == userId);
            if (nguoiDung == null || nguoiDung.MatKhau != currentPassword)
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng." });
            }
            nguoiDung.MatKhau = newPassword;
            db.SubmitChanges();
            return Json(new { success = true, message = "Mật khẩu đã được cập nhật thành công!" });
        }



        public ActionResult GioHang()
        {
            var gioHang = (GioHang)Session["GioHang"] ?? new GioHang();
            double tongTien = gioHang.TongTien();
            ViewBag.TongTien = tongTien;
            ViewBag.SelectedCourseId = TempData["SelectedCourseId"];
            return View(gioHang);
        }


        [HttpPost]
        public ActionResult ThemVaoGioHang(string id)
        {
            var khoaHoc = db.KhoaHocs.FirstOrDefault(kh => kh.MaKhoaHoc == id);

            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                // Nếu chưa đăng nhập, hiển thị thông báo và chuyển đến trang đăng nhập
                TempData["warning"] = "Bạn cần đăng nhập để thực hiện hành động này!";
                return RedirectToAction("DangNhap", "HocVien");
            }

            if (khoaHoc != null)
            {
                // Lấy giỏ hàng từ Session, hoặc tạo mới nếu chưa có
                var gioHang = (GioHang)Session["GioHang"] ?? new GioHang();

                // Kiểm tra khóa học đã có trong giỏ hàng chưa
                if (!gioHang.ct.Where(t => t.kh.MaKhoaHoc == id).Any())
                {
                    gioHang.ThemVaoGio(khoaHoc); // Thêm khóa học vào giỏ
                    Session["GioHang"] = gioHang; // Lưu giỏ hàng vào Session
                    TempData["ToastMessage"] = "Sản phẩm đã được thêm vào Giỏ hàng!";
                }
                else
                {
                    TempData["ToastMessage"] = "Khóa học này đã có trong Giỏ hàng!";
                }

                // Sau khi thêm vào giỏ, quay lại trang chi tiết khóa học
                return RedirectToAction("ChiTietKhoaHoc", "HocVien", new { id = id });
            }

            // Nếu không tìm thấy khóa học
            TempData["ToastMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction("ChiTietKhoaHoc", "HocVien", new { id = id });
        }

        [HttpPost]
        public ActionResult XoaKhoiGioHang(string[] selectedCourses)
        {
            var gioHang = (GioHang)Session["GioHang"] ?? new GioHang();

            // Xóa các khóa học đã chọn trong giỏ
            if (selectedCourses != null)
            {
                foreach (var id in selectedCourses)
                {
                    var sp = gioHang.ct.FirstOrDefault(t => t.kh.MaKhoaHoc == id);
                    if (sp != null)
                        gioHang.ct.Remove(sp);
                }
                Session["GioHang"] = gioHang; // Cập nhật lại giỏ hàng
            }

            return RedirectToAction("GioHang");
        }

        [HttpPost]
        public ActionResult MuaNgay(string id)
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                TempData["warning"] = "Bạn cần đăng nhập để thực hiện hành động này!";
                return RedirectToAction("DangNhap", "HocVien");
            }

            var gioHang = (GioHang)Session["GioHang"] ?? new GioHang();

            // Tìm khóa học theo id
            var khoaHoc = db.KhoaHocs.FirstOrDefault(kh => kh.MaKhoaHoc == id);
            if (khoaHoc != null)
            {
                // Thêm khóa học vào giỏ
                gioHang.ThemVaoGio(khoaHoc);
                Session["GioHang"] = gioHang; // Cập nhật giỏ hàng
            }
            else
            {
                TempData["ToastMessage"] = "Không tìm thấy khóa học.";
            }

            // Chuyển đến trang giỏ hàng
            return RedirectToAction("GioHang", "HocVien");
        }

        [HttpPost]
        public JsonResult CapNhatGiamGia(string maKhoaHoc, string maGiamGia)
        {
            GioHang gh = Session["GioHang"] as GioHang;
            var k = gh.ct.FirstOrDefault(t => t.kh.MaKhoaHoc == maKhoaHoc);
            if (k != null)
            {
                GiamGia g = db.GiamGias.FirstOrDefault(t => t.MaGiamGia == maGiamGia);
                k.gg = g;
            }

            double giaThucTra = k.Tien();
            double tongTien = gh.TongTien();

            Session["GioHang"] = gh;

            return Json(new { success = true, giaThucTra, tongTien });
        }


        // CHI TIET THANH TOAN
        public ActionResult ThongTinThanhToan()
        {
            var maHocVien = Session["UserId"].ToString();
            var thanhToanList = db.ThanhToans.Where(tt => tt.MaHocVien == maHocVien).ToList();

            if (thanhToanList == null || !thanhToanList.Any())
            {
                ViewBag.ThongBao = "Không có dữ liệu thanh toán.";
                return View();
            }
            return View(thanhToanList);
        }
        public ActionResult ChiTietThanhToan()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "HocVien");
            }

            GioHang gh = Session["GioHang"] as GioHang;

            return View(gh);
        }

        [HttpPost]
        public ActionResult UpdateTotal(decimal total)
        {
            Session["TongChiPhi"] = total;
            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult ChonKhoaHocThanhToan(List<string> selectedCourses)
        {
            if (selectedCourses == null || !selectedCourses.Any())
            {
                TempData["Message"] = "Không có khóa học nào được chọn.";
                return RedirectToAction("GioHang");
            }

            // Lấy danh sách các khóa học từ DB theo ID
            var khoaHocDaChon = db.KhoaHocs.Where(kh => selectedCourses.Contains(kh.MaKhoaHoc)).ToList();

            Session["KhoaHocDaChon"] = khoaHocDaChon;

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult HoanTatThanhToan()
        {
            var userId = Session["UserId"] as string;
            GioHang gh = Session["GioHang"] as GioHang;

            if (userId == null)
            {
                TempData["Message"] = "Bạn cần đăng nhập để thanh toán.";
                return RedirectToAction("DangNhap", "HocVien");
            }
            var hocVien = db.HocViens.FirstOrDefault(hv => hv.MaHocVien == userId);
            if (hocVien == null)
            {
                TempData["Message"] = "Người dùng không tồn tại trong hệ thống. Vui lòng kiểm tra tài khoản của bạn.";
                return RedirectToAction("GioHang");
            }

            if (gh == null || gh.ct == null)
            {
                TempData["Message"] = "Không có khóa học nào để thanh toán.";
                return RedirectToAction("GioHang");
            }

            try
            {
                double tongTien = gh.TongTien();
                string maThanhToan = TaoMaThanhToan();
                while (db.ThanhToans.Any(tt => tt.MaThanhToan == maThanhToan))
                {
                    maThanhToan = TaoMaThanhToan();  // Tạo lại mã thanh toán nếu đã tồn tại
                }
                var thanhToan = new ThanhToan
                {
                    MaThanhToan = maThanhToan,
                    MaHocVien = hocVien.MaHocVien,
                    SoTien = decimal.Parse(tongTien.ToString()),
                    NgayThanhToan = DateTime.Now,
                    TrangThai = "Thành công"
                };

                db.ThanhToans.InsertOnSubmit(thanhToan);
                db.SubmitChanges();  // Lưu thanh toán vào cơ sở dữ liệu


                string macuct = "";
                string macudk = "";
                foreach (var item in gh.ct)
                {
                    string maChiTietThanhToan = "";
                    if (macuct == "")
                        maChiTietThanhToan = TaoMaChiTiet();
                    else
                    {
                        string prefix = "CT";  // Tiền tố cho mã thanh toán
                        int numericPart = 0;   // Phần số của mã

                        numericPart = int.Parse(macuct.Substring(2)) + 1;

                        maChiTietThanhToan = prefix + numericPart.ToString("D3");
                    }
                    var chiTietThanhToan = new ChiTietThanhToan
                    {
                        MaChiTiet = maChiTietThanhToan,  // Mã chi tiết thanh toán duy nhất
                        MaThanhToan = thanhToan.MaThanhToan,  // Liên kết với thanh toán
                        SoTien = Decimal.Parse(item.Tien().ToString()),
                        MaGiamGia = item.gg != null ? item.gg.MaGiamGia : null,
                        NgayThucHien = DateTime.Now
                    };

                    db.ChiTietThanhToans.InsertOnSubmit(chiTietThanhToan);
                    macuct = maChiTietThanhToan;
                    string maDangKy = "";
                    if (macudk == "")
                        maDangKy = TaoMaDangKy();
                    else
                    {
                        string prefix = "DK";  // Tiền tố cho mã thanh toán
                        int numericPart = 0;   // Phần số của mã

                        numericPart = int.Parse(macudk.Substring(2)) + 1;

                        maDangKy = prefix + numericPart.ToString("D3");
                    }

                    var dangKy = new DangKy
                    {
                        MaDangKy = TaoMaDangKy(),  // Tạo mã đăng ký tự động
                        MaKhoaHoc = item.kh.MaKhoaHoc,
                        MaChiTiet = chiTietThanhToan.MaChiTiet,  // Liên kết với chi tiết thanh toán
                        TrangThai = "Thanh toán"
                    };

                    db.DangKies.InsertOnSubmit(dangKy);
                    macudk = maDangKy;

                    foreach (var x in db.BaiTaps.Where(t => t.Chuong.MaKhoaHoc == item.kh.MaKhoaHoc))
                    {
                        DangKy_BaiTap dkbt = new DangKy_BaiTap();

                        dkbt.MaDangKy = macudk;
                        dkbt.MaBaiTap = x.MaBaiTap;
                        dkbt.TrangThai = "Chưa nộp";
                        db.DangKy_BaiTaps.InsertOnSubmit(dkbt);
                    }
                    db.SubmitChanges();  // Lưu tất cả thay đổi vào cơ sở dữ liệu
                }
                gh = null;
                Session["GioHang"] = gh;
                TempData["Message"] = "Thanh toán thành công!";
                return RedirectToAction("GioHang");

            }
            catch (Exception ex)
            {
                TempData["Message"] = "Có lỗi xảy ra khi thực hiện thanh toán: " + ex.Message;
                return RedirectToAction("GioHang");
            }
        }

        // Tạo mã thanh toán
        public string TaoMaThanhToan()
        {
            string prefix = "TT";  // Tiền tố cho mã thanh toán
            int numericPart = 0;   // Phần số của mã

            // Lấy mã thanh toán cuối cùng từ cơ sở dữ liệu
            var maThanhToanCuoiCung = db.ThanhToans
                                        .OrderByDescending(tt => tt.MaThanhToan)
                                        .FirstOrDefault();

            if (maThanhToanCuoiCung != null)
            {
                // Lấy phần số của mã thanh toán và tăng giá trị
                numericPart = int.Parse(maThanhToanCuoiCung.MaThanhToan.Substring(2)) + 1;
            }

            string newMaThanhToan = prefix + numericPart.ToString("D3");

            // Kiểm tra mã thanh toán mới có bị trùng với mã đã có trong cơ sở dữ liệu không
            while (db.ThanhToans.Any(tt => tt.MaThanhToan == newMaThanhToan))
            {
                numericPart++; // Tăng giá trị phần số lên
                newMaThanhToan = prefix + numericPart.ToString("D3");
            }

            return newMaThanhToan;  // Trả về mã thanh toán mới
        }


        // Tạo mã chi tiết thanh toán
        public string TaoMaChiTiet(bool tangGiaTri = false)
        {
            string prefix = "CT";  // Tiền tố cho mã chi tiết thanh toán
            int numericPart = 0;   // Phần số của mã

            // Lấy mã chi tiết thanh toán cuối cùng từ cơ sở dữ liệu
            var maChiTietCuoiCung = db.ChiTietThanhToans
                                      .OrderByDescending(ct => ct.MaChiTiet)
                                      .FirstOrDefault();

            if (maChiTietCuoiCung != null)
            {
                // Lấy phần số của mã chi tiết và tăng giá trị nếu cần
                numericPart = int.Parse(maChiTietCuoiCung.MaChiTiet.Substring(2));

                // Nếu tham số tangGiaTri == true, tăng giá trị lên 1
                if (tangGiaTri)
                {
                    numericPart += 1;
                }
                else
                {
                    numericPart++;  // Tăng giá trị phần số mặc định
                }
            }

            string newMaChiTiet = prefix + numericPart.ToString("D3");

            return newMaChiTiet;  // Trả về mã chi tiết thanh toán mới
        }



        // Tạo mã đăng ký
        public string TaoMaDangKy()
        {
            string prefix = "DK";  // Tiền tố cho mã đăng ký
            int numericPart = 0;   // Phần số của mã

            // Lấy mã đăng ký cuối cùng từ cơ sở dữ liệu
            var maDangKyCuoiCung = db.DangKies
                                     .OrderByDescending(dk => dk.MaDangKy)
                                     .FirstOrDefault();

            if (maDangKyCuoiCung != null)
            {
                // Lấy phần số của mã đăng ký và tăng giá trị
                numericPart = int.Parse(maDangKyCuoiCung.MaDangKy.Substring(2)) + 1;
            }

            string newMaDangKy = prefix + numericPart.ToString("D3");

            // Kiểm tra mã đăng ký mới có bị trùng với mã đã có trong cơ sở dữ liệu không
            while (db.DangKies.Any(dk => dk.MaDangKy == newMaDangKy))
            {
                numericPart++; // Tăng giá trị phần số lên
                newMaDangKy = prefix + numericPart.ToString("D3");
            }

            return newMaDangKy;  // Trả về mã đăng ký mới
        }

        // DANG NHAP, DANG KY
        [HttpGet]
        public ActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(string username, string password)
        {
            // Tìm người dùng dựa trên tên đăng nhập và mật khẩu
            var user = db.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username && u.MatKhau == password);

            if (user != null)
            {
                // Kiểm tra mã nhóm người dùng
                if (user.MaNhom == "NN003")
                {
                    // Lưu thông tin người dùng vào session
                    Session["UserId"] = user.MaNguoiDung;
                    Session["User"] = user;
                    Session["UserName"] = user.TenDangNhap;
                    Session["UserEmail"] = user.Email;

                    // Chuyển hướng đến Trang chủ
                    return RedirectToAction("TrangChu", "HocVien");
                }
                else
                {
                    // Trường hợp mã nhóm không phải NN003
                    ViewBag.ErrorMessage = "Bạn không có quyền truy cập!";
                    return View();
                }
            }
            else
            {
                // Sai tên đăng nhập hoặc mật khẩu
                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }
        }


        public ActionResult DangXuat()
        {
            Session.Clear();
            return RedirectToAction("DieuHuong", "Home");
        }

        public string TaoMaNguoiDung()
        {
            string prefix = "ND";  // Tiền tố cho mã đăng ký
            int numericPart = 0;   // Phần số của mã

            // Lấy mã đăng ký cuối cùng từ cơ sở dữ liệu
            var maNguoiDung = db.NguoiDungs
                                     .OrderByDescending(nd=>nd.MaNguoiDung)
                                     .FirstOrDefault();

            if (maNguoiDung != null)
            {
                // Lấy phần số của mã đăng ký và tăng giá trị
                numericPart = int.Parse(maNguoiDung.MaNguoiDung.Substring(2)) + 1;
            }

            string newMaNguoiDung = prefix + numericPart.ToString("D3");

            // Kiểm tra mã đăng ký mới có bị trùng với mã đã có trong cơ sở dữ liệu không
            while (db.NguoiDungs.Any(dk => dk.MaNguoiDung == newMaNguoiDung))
            {
                numericPart++; // Tăng giá trị phần số lên
                newMaNguoiDung = prefix + numericPart.ToString("D3");
            }

            return newMaNguoiDung;  // Trả về mã đăng ký mới
        }

        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(string username, string password, string email)
        {
            try
            {
                NguoiDung nguoiDung = new NguoiDung
                {
                    MaNguoiDung=TaoMaNguoiDung(),
                    TenDangNhap = username,
                    MatKhau = password,
                    Email = email,
                    NgayTao = DateTime.Now,
                    TrangThai = "Hoạt động",
                    MaNhom = "NN003"
                };
                db.NguoiDungs.InsertOnSubmit(nguoiDung);
                db.SubmitChanges();

                string maNguoiDungNew = nguoiDung.MaNguoiDung;
                HocVien hocVien = new HocVien
                {
                    MaHocVien = maNguoiDungNew,
                    HoTen = "Nguoi dung " + maNguoiDungNew,
                    NgaySinh = null,
                    GioiTinh = null,
                    SoDienThoai = null,
                    DiaChi = null
                };
                db.HocViens.InsertOnSubmit(hocVien);
                db.SubmitChanges();

                TempData["SuccessMessage"] = "Đăng ký thành công!";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đăng ký thất bại: " + ex.Message;
                return View();
            }
        }
    }
}
