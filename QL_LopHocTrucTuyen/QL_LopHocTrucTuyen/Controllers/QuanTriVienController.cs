using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_LopHocTrucTuyen.Models;
using System.IO;
using System.Data.Entity;
using System.Web.Security;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace QL_LopHocTrucTuyen.Controllers
{
    public class QuanTriVienController : Controller
    {
        DataClasses1DataContext db = new DataClasses1DataContext();

        public string TaoMa(string macu)
        {
            string prefix = macu.Substring(0, 2);
            int numericPart = int.Parse(macu.Substring(2));
            string mamoi = prefix + (numericPart + 1).ToString("D3");

            return mamoi;
        }

        [AllowAnonymous]
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult DangNhap(FormCollection col)
        {
            string tk = col["inputTenTK"];
            string mk = col["inputMatKhau"];

            ViewBag.tk = tk;
            ViewBag.mk = mk;

            NguoiDung user = db.NguoiDungs.FirstOrDefault(k => k.TenDangNhap == tk && k.MatKhau == mk);
            if (string.IsNullOrEmpty(tk) || string.IsNullOrEmpty(mk))
            {
                ViewBag.text = "Vui lòng điền đầy đủ thông tin!";
                return View();
            } else if (!string.IsNullOrEmpty(tk) && tk.Contains(" "))
            {
                ViewBag.text = "Tên đăng nhập không chứa khoảng trắng!";
                return View();
            }
            if (user == null)
            {
                ViewBag.text = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }

            if (user.TrangThai == "Đã khoá" && ( user.MaNhom == "NN004" || user.MaNhom == "NN005"))
            {
                ViewBag.text = "Tài khoản đã bị đình chỉ!";
                return View();
            }
            else if (user.TrangThai == "Hoạt động" && (user.MaNhom == "NN001" || user.MaNhom == "NN004" || user.MaNhom == "NN005"))
            {
                Session["user"] = user;
                TempData["ThongTin"] = "Đăng nhập thành công";
                FormsAuthentication.SetAuthCookie(user.TenDangNhap, false);
                return RedirectToAction("BangDieuKhien", "QuanTriVien");
            }
            else if (user.MaNhom == "NN002" || user.MaNhom == "NN003")
            {
                ViewBag.text = "Tài khoản không đủ quyền truy cập!";
                return View();
            }
            return View();
        }

        public ActionResult DangXuat()
        {
            if (Session["user"] != null)
            {
                Session["user"] = null;
            }
            return RedirectToAction("DangNhap");
        }

        public ActionResult BangDieuKhien()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            // Gọi stored procedure để lấy số lượng học viên và giảng viên
            var soLuongHocVien = db.HocViens.Count();
            var soLuongGiangVien = db.GiangViens.Count();
            var soLuongKhoaHoc = db.KhoaHocs.Count();

            // Trả về kết quả vào view
            ViewBag.SoLuongHocVien = soLuongHocVien;
            ViewBag.SoLuongGiangVien = soLuongGiangVien;
            ViewBag.SoLuongKhoaHoc = soLuongKhoaHoc;
            return View();
        }

        public ActionResult GetThongKeSoTuoi()
        {
            // SQL truy vấn tính toán số tuổi và số lượng
            var sql = @"
        SELECT 
            DATEDIFF(YEAR, H.NgaySinh, GETDATE()) - 
            CASE 
                WHEN MONTH(H.NgaySinh) > MONTH(GETDATE()) 
                     OR (MONTH(H.NgaySinh) = MONTH(GETDATE()) AND DAY(H.NgaySinh) > DAY(GETDATE()))
                THEN 1 
                ELSE 0 
            END AS Tuoi,
            COUNT(*) AS SoLuong
        FROM 
            HocVien H
        GROUP BY 
            DATEDIFF(YEAR, H.NgaySinh, GETDATE()) - 
            CASE 
                WHEN MONTH(H.NgaySinh) > MONTH(GETDATE()) 
                     OR (MONTH(H.NgaySinh) = MONTH(GETDATE()) AND DAY(H.NgaySinh) > DAY(GETDATE()))
                THEN 1 
                ELSE 0 
            END";

            var result = db.ExecuteQuery<QuanTriVienModel.ThongKeSoTuoi>(sql).ToList();

            // Chuyển dữ liệu sang JSON
            var jsonResult = JsonConvert.SerializeObject(result);
            ViewBag.SoTuoi = jsonResult;

            return PartialView(result);
        }

        public ActionResult GetDangKyTheoThang()
        {
            var sql = @" SELECT 
        FORMAT(CT.NgayThucHien, 'M/yyyy') AS ThangNam,
        COUNT(DK.MaDangKy) AS SoLuongDangKy      
    FROM 
        ChiTietThanhToan CT
    JOIN 
        DangKy DK ON CT.MaChiTiet = DK.MaChiTiet    
    WHERE 
        CT.NgayThucHien IS NOT NULL                
    GROUP BY 
        FORMAT(CT.NgayThucHien, 'M/yyyy')         
    ORDER BY 
        MIN(CT.NgayThucHien)  ";
            var result = db.ExecuteQuery<QuanTriVienModel.SoluongDangKyTheoThangResult>(sql).ToList();
            return PartialView(result);
        }

        public ActionResult GetThongKeKhoaHocTheoTungLoai()
        {
            var sql = @"SELECT 
                    LK.TenLoai AS TenLoaiKhoaHoc, 
                    COUNT(KH.MaKhoaHoc) AS SoKhoaHoc
                FROM 
                    LoaiKhoaHoc LK
                LEFT JOIN 
                    KhoaHoc KH ON LK.MaLoaiKhoaHoc = KH.MaLoaiKhoaHoc
                GROUP BY 
                    LK.TenLoai;
                ";
            var result = db.ExecuteQuery<QuanTriVienModel.KhoaHocTheoLoai>(sql).ToList();
            return PartialView(result);
        }

        public ActionResult GetDoanhThuTheoThang()
        {
            // Khởi tạo DataContext
            using (var db = new DataClasses1DataContext()) // Thay YourDataContext bằng tên lớp DataContext của bạn
            {
                // Truyền câu lệnh SELECT trực tiếp
                string sql = @"
                    SELECT 
                        FORMAT(TT.NgayThanhToan, 'MM/yyyy') AS ThangNam, -- Định dạng tháng/năm
                        SUM(CTT.SoTien) AS DoanhThu                     -- Tổng doanh thu
                    FROM 
                        ThanhToan TT
                    JOIN 
                        ChiTietThanhToan CTT ON TT.MaThanhToan = CTT.MaThanhToan -- Liên kết bảng chi tiết thanh toán
                    JOIN 
                        DangKy DK ON CTT.MaChiTiet = DK.MaChiTiet       -- Liên kết bảng đăng ký
                    WHERE 
                        TT.NgayThanhToan IS NOT NULL                   -- Chỉ lấy các bản ghi có ngày thanh toán
                    GROUP BY 
                        FORMAT(TT.NgayThanhToan, 'MM/yyyy')            -- Nhóm theo tháng/năm
                    ORDER BY 
                        MIN(TT.NgayThanhToan)";                        

                // Thực thi SQL và ánh xạ vào model
                var result = db.ExecuteQuery<QuanTriVienModel.DoanhThuTheoThang>(sql).ToList();

                // Chuyển dữ liệu sang chuỗi JSON
                var jsonResult = JsonConvert.SerializeObject(result);
                ViewBag.DoanhThuData = jsonResult;

                // Trả về PartialView với dữ liệu
                return PartialView(result);
            }
        }

        public ActionResult QuanTriVien(int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsAdmin = db.QuanTriViens.ToList();
            var totalRecords = dsAdmin.Count();

            var adminList = dsAdmin.OrderBy(q => q.MaQuanTriVien).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.TenNhom = db.NhomNguoiDungs.ToList();

            var model = new QuanTriVienModel.QuanTriVienPagedList
            {
                AdminList = adminList,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult TimKiemQuanTriVien(string search, int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            ViewBag.TenNhom = db.NhomNguoiDungs.ToList();
            var dsAdmin = db.QuanTriViens.ToList();

            // Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                dsAdmin = dsAdmin.Where(q => q.HoTen.ToLower().Contains(search.ToLower())).ToList();
            }

            var totalRecords = dsAdmin.Count();

            var adminList = dsAdmin.OrderBy(q => q.MaQuanTriVien).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var model = new QuanTriVienModel.QuanTriVienPagedList
            {
                AdminList = adminList,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                SearchQuery = search // Truyền searchQuery vào model
            };

            return View(model);
        }

        public ActionResult ThemQuanTriVien()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var model = new QuanTriVien();
            return View(model);
        }
        [HttpPost]
        public ActionResult ThemQuanTriVien(QuanTriVien user, HttpPostedFileBase AnhDaiDien)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            try
            {
                string fileName = Path.GetFileName(AnhDaiDien.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                AnhDaiDien.SaveAs(path); // lưu vào dự án
                user.NguoiDung.AnhDaiDien = fileName; // Gán giá trị file upload

                var findUser = db.NguoiDungs.FirstOrDefault(t => t.TenDangNhap == user.NguoiDung.TenDangNhap);
                if (findUser != null)
                {
                    ViewBag.errTenDangNhap = "Tên đăng nhập đã tồn tại!";
                    return View(user);
                }

                if (user.NguoiDung.MaNhom == "")
                {
                    ViewBag.errMaNhom = "Vui lòng chọn nhóm người dùng!";
                    return View(user); 
                }

                var nguoiDung = new NguoiDung
                {
                    MaNguoiDung = TaoMa(db.NguoiDungs.OrderByDescending(t => t.MaNguoiDung).FirstOrDefault().MaNguoiDung),
                    TenDangNhap = user.NguoiDung.TenDangNhap,
                    MatKhau = user.NguoiDung.MatKhau,
                    Email = user.NguoiDung.Email,
                    NgayTao = DateTime.Now,
                    TrangThai = "Hoạt động",
                    AnhDaiDien = user.NguoiDung.AnhDaiDien,
                    MaNhom = user.NguoiDung.MaNhom
                };
                string cv;
                if (nguoiDung.MaNhom == "NN001")
                {
                    cv = "Trưởng phòng quản trị";
                }
                else if (nguoiDung.MaNhom == "NN004")
                {
                    cv = "Thu ngân hệ thống";
                }
                else
                {
                    cv = "Kỹ thuật viên hệ thống";
                }

                db.NguoiDungs.InsertOnSubmit(nguoiDung);
                db.SubmitChanges();

                var quanTriVien = new QuanTriVien
                {
                    MaQuanTriVien = nguoiDung.MaNguoiDung,
                    HoTen = user.HoTen,
                    ChucVu = cv
                };
                db.QuanTriViens.InsertOnSubmit(quanTriVien);
                db.SubmitChanges();
                TempData["SuccessMessage"] = "Tạo quản trị viên thành công";
                return RedirectToAction("QuanTriVien");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(user);
            }
        }

        public ActionResult ChiTietQuanTriVien(string id)
        {
            var quantrivien = db.QuanTriViens.FirstOrDefault(t => t.MaQuanTriVien == id);
            return View(quantrivien);
        }
        [HttpPost]
        public ActionResult ChiTietQuanTriVien(QuanTriVien user, HttpPostedFileBase AnhDaiDien)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var findUser = db.QuanTriViens.FirstOrDefault(t => t.NguoiDung.MaNguoiDung == user.NguoiDung.MaNguoiDung);
                if (findUser == null)
                {
                    return View(user); // Trả về lại trang với model để hiển thị lỗi
                }
                if (AnhDaiDien != null && AnhDaiDien.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(AnhDaiDien.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                    AnhDaiDien.SaveAs(path); // lưu vào dự án
                    user.NguoiDung.AnhDaiDien = fileName; // Gán giá trị file upload
                }
                if (string.IsNullOrEmpty(user.NguoiDung.AnhDaiDien))
                {
                    user.NguoiDung.AnhDaiDien = findUser.NguoiDung.AnhDaiDien;
                }
                // Cập nhật các thông tin của người dùng
                findUser.NguoiDung.MatKhau = user.NguoiDung.MatKhau;
                findUser.NguoiDung.Email = user.NguoiDung.Email;

                findUser.NguoiDung.AnhDaiDien = user.NguoiDung.AnhDaiDien;
                findUser.NguoiDung.MaNhom = user.NguoiDung.MaNhom;

                if (findUser.NguoiDung.MaNhom == "NN001")
                {
                    findUser.NguoiDung.TrangThai = "Hoạt động";
                    findUser.ChucVu = "Quản trị viên";
                }
                else
                {
                    findUser.NguoiDung.TrangThai = user.NguoiDung.TrangThai;
                    findUser.ChucVu = (user.NguoiDung.MaNhom == "NN004") ? "Thu ngân hệ thống" : "Kĩ thuật viên hệ thống";
                }
                findUser.HoTen = user.HoTen;

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật quản trị viên thành công";
                return RedirectToAction("QuanTriVien");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(user);
            }
        }

        public ActionResult GiangVien(int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsInstructor = db.GiangViens.ToList();
            var totalRecords = dsInstructor.Count();
            var instructorList = dsInstructor.OrderBy(q => q.MaGiangVien).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var model = new QuanTriVienModel.GiangVienPagedList
            {
                InstructorList = instructorList,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(model);
        }
        [HttpGet]
        public ActionResult TimKiemGiangVien(string search, int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsInstructor = db.GiangViens.ToList();
            // Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                dsInstructor = dsInstructor.Where(q => q.HoTen.ToLower().Contains(search.ToLower())).ToList();
            }

            var totalRecords = dsInstructor.Count();
            var instructorList = dsInstructor.OrderBy(q => q.MaGiangVien).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var model = new QuanTriVienModel.GiangVienPagedList
            {
                InstructorList = instructorList,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                SearchQuery = search
            };

            return View(model);
        }

        public ActionResult ThemGiangVien()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var model = new GiangVien();
            return View(model);
        }
        [HttpPost]
        public ActionResult ThemGiangVien(GiangVien user, HttpPostedFileBase AnhDaiDien)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            try
            {
                string fileName = Path.GetFileName(AnhDaiDien.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                AnhDaiDien.SaveAs(path); // lưu vào dự án
                user.NguoiDung.AnhDaiDien = fileName; // Gán giá trị file upload

                var findUser = db.NguoiDungs.FirstOrDefault(t => t.TenDangNhap == user.NguoiDung.TenDangNhap);
                if (findUser != null)
                {
                    ViewBag.errTenDangNhap1 = "Tên đăng nhập đã tồn tại!";
                    return View(user);  // Trả về view với thông báo lỗi
                }

                var nguoiDung = new NguoiDung
                {
                    MaNguoiDung = TaoMa(db.NguoiDungs.OrderByDescending(t => t.MaNguoiDung).FirstOrDefault().MaNguoiDung),
                    TenDangNhap = user.NguoiDung.TenDangNhap,
                    MatKhau = user.NguoiDung.MatKhau,
                    Email = user.NguoiDung.Email,
                    NgayTao = DateTime.Now,
                    TrangThai = "Hoạt động",
                    AnhDaiDien = user.NguoiDung.AnhDaiDien,
                    MaNhom = "NN002"
                };
                db.NguoiDungs.InsertOnSubmit(nguoiDung);
                db.SubmitChanges();

                var giangVien = new GiangVien
                {
                    MaGiangVien = nguoiDung.MaNguoiDung,
                    HoTen = user.HoTen,
                    ChuyenNganh = user.ChuyenNganh,
                    SoDienThoai = user.SoDienThoai,
                    NgayGiaNhap = user.NgayGiaNhap,
                    DiaChi = user.DiaChi
                };
                db.GiangViens.InsertOnSubmit(giangVien);
                db.SubmitChanges();
                TempData["SuccessMessage"] = "Tạo giảng viên thành công";
                return RedirectToAction("GiangVien");

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(user);
            }
        }

        public ActionResult ChiTietGiangVien(string id)
        {
            var giangvien = db.GiangViens.FirstOrDefault(t => t.MaGiangVien == id);
            return View(giangvien);
        }
        [HttpPost]
        public ActionResult ChiTietGiangVien(GiangVien user, HttpPostedFileBase AnhDaiDien)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var findUser = db.GiangViens.FirstOrDefault(t => t.NguoiDung.MaNguoiDung == user.NguoiDung.MaNguoiDung);
                if (findUser == null)
                {
                    return View(user); // Trả về lại trang với model để hiển thị lỗi
                }
                if (AnhDaiDien != null && AnhDaiDien.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(AnhDaiDien.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                    AnhDaiDien.SaveAs(path); // lưu vào dự án
                    user.NguoiDung.AnhDaiDien = fileName; // Gán giá trị file upload
                }
                if (string.IsNullOrEmpty(user.NguoiDung.AnhDaiDien))
                {
                    user.NguoiDung.AnhDaiDien = findUser.NguoiDung.AnhDaiDien;
                }
                findUser.NguoiDung.MatKhau = user.NguoiDung.MatKhau;
                findUser.NguoiDung.Email = user.NguoiDung.Email;
                findUser.NguoiDung.MaNhom = "NN002";

                findUser.NguoiDung.AnhDaiDien = user.NguoiDung.AnhDaiDien;
                findUser.NguoiDung.TrangThai = user.NguoiDung.TrangThai;
                findUser.HoTen = user.HoTen;
                findUser.ChuyenNganh = user.ChuyenNganh;
                findUser.SoDienThoai = user.SoDienThoai;
                findUser.DiaChi = user.DiaChi;

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật giảng viên thành công";
                return RedirectToAction("GiangVien");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(user);
            }
        }

        public ActionResult XoaGiangVien(string id)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var giangVien = db.GiangViens.FirstOrDefault(k => k.MaGiangVien == id);
                if (giangVien != null)
                {
                    db.GiangViens.DeleteOnSubmit(giangVien);
                    db.SubmitChanges();
                }
                return RedirectToAction("GiangVien");

            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Không thể xóa giảng viên này";
                return RedirectToAction("GiangVien");
            }
        }

        public ActionResult HocVien(int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsStudent= db.HocViens.ToList();
            var totalRecords = dsStudent.Count();
            var studentList = dsStudent.OrderBy(q => q.MaHocVien).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var model = new QuanTriVienModel.HocVienPagedList
            {
                StudentList = studentList,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
            return View(model);
        }

        [HttpGet]
        public ActionResult TimKiemHocVien(string search, int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsStudent = db.HocViens.ToList();

            if (!string.IsNullOrEmpty(search))
            {
                dsStudent = dsStudent.Where(t => t.HoTen.ToLower().Contains(search.ToLower())).ToList();
            }

            var totalRecords = dsStudent.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var studentList = dsStudent.OrderBy(q => q.MaHocVien).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new QuanTriVienModel.HocVienPagedList
            {
                StudentList = studentList,
                PageSize = pageSize,
                TotalPages = totalPages,
                CurrentPage = page,
                SearchQuery = search
            };

            return View(model);
        }

        public ActionResult ThemHocVien()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var model = new HocVien();
            return View(model);
        }
        [HttpPost]
        public ActionResult ThemHocVien(HocVien user, HttpPostedFileBase AnhDaiDien)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            try
            {
                string fileName = Path.GetFileName(AnhDaiDien.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                AnhDaiDien.SaveAs(path); // lưu vào dự án
                user.NguoiDung.AnhDaiDien = fileName; // Gán giá trị file upload

                var findUser = db.NguoiDungs.FirstOrDefault(t => t.TenDangNhap == user.NguoiDung.TenDangNhap);
                if (findUser != null)
                {
                    ViewBag.errTenDangNhap1 = "Tên đăng nhập đã tồn tại!";
                    return View(user);  // Trả về view với thông báo lỗi
                }

                var nguoiDung = new NguoiDung
                {
                    MaNguoiDung = TaoMa(db.NguoiDungs.OrderByDescending(t => t.MaNguoiDung).FirstOrDefault().MaNguoiDung),
                    TenDangNhap = user.NguoiDung.TenDangNhap,
                    MatKhau = user.NguoiDung.MatKhau,
                    Email = user.NguoiDung.Email,
                    NgayTao = DateTime.Now,
                    TrangThai = "Hoạt động",
                    AnhDaiDien = user.NguoiDung.AnhDaiDien,
                    MaNhom = "NN003"
                };
                db.NguoiDungs.InsertOnSubmit(nguoiDung);
                db.SubmitChanges();

                var hocVien = new HocVien
                {
                    MaHocVien = nguoiDung.MaNguoiDung,
                    HoTen = user.HoTen,
                    NgaySinh = user.NgaySinh,
                    GioiTinh = user.GioiTinh,
                    SoDienThoai = user.SoDienThoai,
                    DiaChi = user.DiaChi,
                    NgayDangKy = user.NgayDangKy
                };
                db.HocViens.InsertOnSubmit(hocVien);
                db.SubmitChanges();
                TempData["SuccessMessage"] = "Tạo học viên thành công";
                return RedirectToAction("HocVien");

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(user);
            }
        }

        public ActionResult ChiTietHocVien(string id)
        {
            var hocVien = db.HocViens.FirstOrDefault(t => t.MaHocVien == id);
            return View(hocVien);
        }

        [HttpPost]
        public ActionResult ChiTietHocVien(HocVien user, HttpPostedFileBase AnhDaiDien)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var findUser = db.HocViens.FirstOrDefault(t => t.NguoiDung.MaNguoiDung == user.NguoiDung.MaNguoiDung);
                if (findUser == null)
                {
                    return View(user); // Trả về lại trang với model để hiển thị lỗi
                }
                if (AnhDaiDien != null && AnhDaiDien.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(AnhDaiDien.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                    AnhDaiDien.SaveAs(path); // lưu vào dự án
                    user.NguoiDung.AnhDaiDien = fileName; // Gán giá trị file upload
                }
                if (string.IsNullOrEmpty(user.NguoiDung.AnhDaiDien))
                {
                    user.NguoiDung.AnhDaiDien = findUser.NguoiDung.AnhDaiDien;
                }
                findUser.NguoiDung.MatKhau = user.NguoiDung.MatKhau;
                findUser.NguoiDung.Email = user.NguoiDung.Email;
                findUser.NguoiDung.MaNhom = "NN003";

                findUser.NguoiDung.AnhDaiDien = user.NguoiDung.AnhDaiDien;
                findUser.NguoiDung.TrangThai = user.NguoiDung.TrangThai;
                findUser.HoTen = user.HoTen;
                findUser.GioiTinh = user.GioiTinh;
                findUser.NgaySinh = user.NgaySinh;
                findUser.SoDienThoai = user.SoDienThoai;
                findUser.DiaChi = user.DiaChi;
                findUser.NgayDangKy = user.NgayDangKy;

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật học viên thành công";
                return RedirectToAction("HocVien");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(user);
            }
        }
        public ActionResult XoaHocVien(string id)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var hocVien = db.HocViens.FirstOrDefault(k => k.MaHocVien == id);
                if (hocVien != null)
                {
                    db.HocViens.DeleteOnSubmit(hocVien);
                    db.SubmitChanges();
                }
                return RedirectToAction("HocVien");

            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Không thể xóa học viên này";
                return RedirectToAction("HocVien");
            }
        }
        public ActionResult LoaiKhoaHoc(int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsLoaiKH = db.LoaiKhoaHocs.ToList();
            var totalRecords = dsLoaiKH.Count();

            var dsLoaiKHList = dsLoaiKH.OrderBy(q => q.MaLoaiKhoaHoc).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var model = new QuanTriVienModel.LoaiKhoaHocPagedList
            {
                LoaiKhoaHoc = dsLoaiKHList,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        public ActionResult TimKiemLoaiKhoaHoc(string search, int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var dsLoaiKH = db.LoaiKhoaHocs.ToList();

            if (!string.IsNullOrEmpty(search))
            {
                dsLoaiKH = dsLoaiKH.Where(t => t.TenLoai.ToLower().Contains(search.ToLower())).ToList();
            }

            var totalRecords = dsLoaiKH.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var khoahocList = dsLoaiKH.OrderBy(q => q.MaLoaiKhoaHoc).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new QuanTriVienModel.LoaiKhoaHocPagedList
            {
                LoaiKhoaHoc = khoahocList,
                PageSize = pageSize,
                TotalPages = totalPages,
                CurrentPage = page,
                SearchQuery = search
            };

            return View(model);
        }

        public ActionResult ChiTietLoaiKhoaHoc(string id)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            LoaiKhoaHoc loaiKH = db.LoaiKhoaHocs.FirstOrDefault(t => t.MaLoaiKhoaHoc == id);
            return View(loaiKH);
        }
        [HttpPost]
        public ActionResult ChiTietLoaiKhoaHoc(LoaiKhoaHoc loai)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var findLoai = db.LoaiKhoaHocs.FirstOrDefault(t => t.MaLoaiKhoaHoc == loai.MaLoaiKhoaHoc);
                if (findLoai == null)
                {
                    return View(loai); // Trả về lại trang với model để hiển thị lỗi
                }
                findLoai.MaLoaiKhoaHoc = loai.MaLoaiKhoaHoc;
                findLoai.TenLoai = loai.TenLoai;
                findLoai.MoTa = loai.MoTa;

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật loại khóa học thành công";
                return RedirectToAction("LoaiKhoaHoc");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(loai);
            }
        }

        public ActionResult DanhSachKhoaHocThuocLoaiKH(string id)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            List<KhoaHoc> listKH = db.KhoaHocs.Where(t => t.MaLoaiKhoaHoc == id).ToList();
            return PartialView(listKH);
        }

        public ActionResult XoaLoaiKhoaHoc(string id)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                var loai = db.LoaiKhoaHocs.FirstOrDefault(k => k.MaLoaiKhoaHoc == id);
                if (loai != null)
                {
                    db.LoaiKhoaHocs.DeleteOnSubmit(loai);
                    db.SubmitChanges();
                }
                return RedirectToAction("LoaiKhoaHoc");

            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Không thể xóa loại khóa học này";
                return RedirectToAction("LoaiKhoaHoc");
            }
        }

        public ActionResult ThemLoaiKhoaHoc()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ThemLoaiKhoaHoc(LoaiKhoaHoc kh)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try {
                var addLoaiKH = new LoaiKhoaHoc
                {
                    MaLoaiKhoaHoc = TaoMa(db.LoaiKhoaHocs.OrderByDescending(t => t.MaLoaiKhoaHoc).FirstOrDefault().MaLoaiKhoaHoc),
                    TenLoai = kh.TenLoai,
                    MoTa = kh.MoTa
                };
                db.LoaiKhoaHocs.InsertOnSubmit(addLoaiKH);
                db.SubmitChanges();
                TempData["SuccessMessage"] = "Tạo loại khóa học thành công";
                return RedirectToAction("LoaiKhoaHoc");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(kh);
            }
        }
        public ActionResult KhoaHoc()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            List<KhoaHoc> listKhoaHoc = db.KhoaHocs.OrderByDescending(t => t.MaKhoaHoc).ToList();
            return View(listKhoaHoc);
        }

        public ActionResult ChiTietKhoaHoc(string makh)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            KhoaHoc KhoaHoc = db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc == makh);
            return View(KhoaHoc);
        }

        public ActionResult XuLyDieuHuong(string makh, string page)
        {
            TempData["DieuHuong"] = page;
            return RedirectToAction("ChiTietKhoaHoc", new { makh = makh });
        }

        public ActionResult ThongTinKhoaHoc(string makh)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            KhoaHoc KhoaHoc = db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc == makh);
            int chuong = db.Chuongs.Count(c => c.MaKhoaHoc == makh);
            ViewBag.SoLuongChuong = chuong;
            int soLuongBaiGiang = db.BaiGiangs.Count(b => db.Chuongs.Any(c => c.MaChuong == b.MaChuong && c.MaKhoaHoc == makh));
            ViewBag.SoLuongBaiGiang = soLuongBaiGiang;

            int soLuongBaiTap = db.BaiTaps.Count(b => db.Chuongs.Any(c => c.MaChuong == b.MaChuong && c.MaKhoaHoc == makh));
            ViewBag.SoLuongBaiTap = soLuongBaiTap;
            return PartialView(KhoaHoc);
        }

        public ActionResult BaiHoc(string makh)
        {
            KhoaHoc kh = db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc == makh);
            if(kh.Chuongs.FirstOrDefault() != null && kh.Chuongs.FirstOrDefault().BaiGiangs.FirstOrDefault() != null)
            {
                ViewBag.bgdt = kh.Chuongs.FirstOrDefault().BaiGiangs.FirstOrDefault().MaBaiGiang;
            }

            if (kh.Chuongs.FirstOrDefault() != null && kh.Chuongs.FirstOrDefault().BaiTaps.FirstOrDefault() != null)
            {
                ViewBag.btdt = kh.Chuongs.FirstOrDefault().BaiTaps.FirstOrDefault().MaBaiTap;
            }
            return PartialView(kh);
        }

        public ActionResult XemBaiGiang(string mabg)
        {
            var baiGiang = db.BaiGiangs.FirstOrDefault(t => t.MaBaiGiang == mabg);
            if (baiGiang == null)
            {
                return new HttpStatusCodeResult(404, "Bài giảng không tồn tại");
            }
            return PartialView("_XemBaiGiangPartial", baiGiang); // Trả về partial view kèm dữ liệu
        }

        public ActionResult XemDanhGia(string makh)
        {
            List<DanhGia> dg = db.DanhGias.Where(t => t.MaKhoaHoc == makh).ToList();
            return PartialView(dg);
        }
        public ActionResult XemBaiTap(string mabt)
        {
            var baitap = db.BaiTaps.FirstOrDefault(t => t.MaBaiTap == mabt);
            if (baitap == null)
            {
                return new HttpStatusCodeResult(404, "Bài tập không tồn tại");
            }
            return PartialView("_XemBaiTapPartial", baitap);    
        }

        public ActionResult HocVienKhoaHoc(string makh)
        {
            List<HocVien> lst = db.HocViens.Where(hv => hv.ThanhToans.
                Where(t => t.ChiTietThanhToans.
                    Where(ct => ct.DangKies.Where(dk => dk.MaKhoaHoc == makh).Any()).Any()).Any()).ToList();

            ViewBag.MaKhoaHoc = makh;
            ViewBag.TongBaiGiang = db.BaiGiangs.Where(t => t.Chuong.MaKhoaHoc.ToString() == makh).Count();

            return PartialView(lst);
        }

        public ActionResult BaoCaoKhoaHoc(string makh)
        {
            return PartialView(db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh));
        }

        public ActionResult BaiTap(string makh)
        {
            return PartialView(db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh));
        }

        public ActionResult ChamDiem(string mabt, string madk)
        {
            return View(db.DangKy_BaiTaps.FirstOrDefault(t => t.MaBaiTap.ToString() == mabt && t.MaDangKy.ToString() == madk));
        }

        public ActionResult ThietLap(string makh)
        {
            ViewBag.LoaiKhoaHocs = db.LoaiKhoaHocs.ToList();
            return PartialView(db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh));
        }

        public ActionResult PheDuyetKhoaHoc(string makh)
        {
            TempData["DieuHuong"] = "ThietLap";
            var k = db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc == makh);
            k.TrangThai = "Đã duyệt";
            db.SubmitChanges();
            TempData["Info"] = "Khoá học đã hoạt động";
            return RedirectToAction("KhoaHoc", new { makh = makh });
        }

        public ActionResult TuChoiPheDuyet(string makh)
        {
            TempData["DieuHuong"] = "ThietLap";
            var k = db.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc == makh);
            k.TrangThai = "Từ chối phê duyệt";
            db.SubmitChanges();
            TempData["Info"] = "Khoá học đã bị từ chối phê duyệt";
            return RedirectToAction("KhoaHoc", new { makh = makh });
        }

        public ActionResult DanhSachMaGiamGia(int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            List<GiamGia> giamGiaList = db.GiamGias.ToList();
            var totalRecords = giamGiaList.Count();

            var dsGiamGiaList = giamGiaList.OrderBy(q => q.MaGiamGia).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var model = new QuanTriVienModel.GiamGiaPagedList
            {
                giamGiaList = dsGiamGiaList,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };
            return View(model);
        }
        [HttpGet]
        public ActionResult TimKiemGiamGia(string search, int page = 1, int pageSize = 5)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            List<GiamGia> giamGiaList = db.GiamGias.ToList();
            // Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                giamGiaList = giamGiaList.Where(q => q.TenGiamGia.ToLower().Contains(search.ToLower())).ToList();
            }
            var totalRecords = giamGiaList.Count();

            var dsGiamGiaList = giamGiaList.OrderBy(q => q.MaGiamGia).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var model = new QuanTriVienModel.GiamGiaPagedList
            {
                giamGiaList = dsGiamGiaList,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                SearchQuery = search
            };
            return View(model);
        }
        public ActionResult ChiTietGiamGia(string maGG)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            GiamGia giamGia = db.GiamGias.FirstOrDefault(t => t.MaGiamGia == maGG);
            return View(giamGia);
        }
        [HttpPost]
        public ActionResult ChiTietGiamGia(GiamGia giamgia)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var giamGiaExists = db.GiamGias.FirstOrDefault(k => k.MaGiamGia == giamgia.MaGiamGia);
            if (giamGiaExists != null)
            {
                giamGiaExists.TenGiamGia = giamgia.TenGiamGia;
                giamGiaExists.NgayBatDau = giamgia.NgayBatDau;
                giamGiaExists.NgayKetThuc = giamgia.NgayKetThuc;
                giamGiaExists.PhanTramGiam = giamgia.PhanTramGiam;
                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật mã giảm giá thành công";
                return RedirectToAction("DanhSachMaGiamGia");
            }
            return View(giamgia);
        }

        public ActionResult ThemGiamGia()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var giamGia = new GiamGia();
            return View(giamGia);
        }
        [HttpPost]
        public ActionResult ThemGiamGia(GiamGia gg)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            try
            {
                string maGiamGia = db.GiamGias.OrderByDescending(t => t.MaGiamGia).FirstOrDefault().MaGiamGia;
                int k = maGiamGia != null ? (int.Parse(maGiamGia.Substring(2)) + 1) : 1;
                string maGiamGiaMoi = "GG" + k.ToString("D3");
                var giamGia = new GiamGia
                {
                    MaGiamGia = maGiamGiaMoi,
                    TenGiamGia = gg.TenGiamGia,
                    PhanTramGiam = gg.PhanTramGiam,
                    NgayBatDau = gg.NgayBatDau,
                    NgayKetThuc = gg.NgayKetThuc
                };
                TempData["SuccessMessage"] = "Thêm mã giảm giá thành công!" ;
                db.GiamGias.InsertOnSubmit(giamGia);
                db.SubmitChanges();

                return RedirectToAction("DanhSachMaGiamGia");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(gg);
            }
        }

        public ActionResult XoaGiamGia(string maGG)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            GiamGia giamGia = db.GiamGias.FirstOrDefault(k => k.MaGiamGia == maGG);
            if (giamGia.KhoaHoc_GiamGias.Any())
            {
                TempData["ErrorMessage"] = "Mã Giảm giá đang được sử dụng, nên không thể xóa!";
                return RedirectToAction("ChiTietGiamGia", new { maGG = maGG });
            }
            db.GiamGias.DeleteOnSubmit(giamGia);
            db.SubmitChanges();
            TempData["SuccessMessage"] = "Xóa mã giảm giá thành công!";
            return RedirectToAction("DanhSachMaGiamGia");
        }

        public ActionResult DanhSachGiamGiaThuocKhoaHoc(string maGG)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            ViewBag.MaGiamGia = maGG;
            GiamGia checkTrangThaiGiamGia = db.GiamGias.FirstOrDefault(t => t.MaGiamGia == maGG);
            if (checkTrangThaiGiamGia.NgayKetThuc < DateTime.Now)
            {
                ViewBag.TrangThaiKhoaHoc = "Mã giảm giá hết hạn, nên không thể thêm khóa học";
            }
            List<KhoaHoc_GiamGia> dsGiamGiaThuocKhoaHoc = db.KhoaHoc_GiamGias.Where(k => k.MaGiamGia == maGG).ToList();
            return PartialView(dsGiamGiaThuocKhoaHoc);
        }

        public ActionResult XoaKhoaHoc_GiamGia(string maKH, string maGG)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            KhoaHoc_GiamGia khoaHoc_giamGia = db.KhoaHoc_GiamGias.FirstOrDefault(t => t.MaGiamGia == maGG && t.MaKhoaHoc == maKH);
            db.KhoaHoc_GiamGias.DeleteOnSubmit(khoaHoc_giamGia);
            db.SubmitChanges();
            return RedirectToAction("ChiTietGiamGia", new { maGG = maGG });
        }
        public ActionResult ThemKhoaHoc_GiamGia(string maGG)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            ViewBag.MaGiamGia = maGG;
            List<GiamGia> giamGiaList = db.GiamGias.ToList();
            List<KhoaHoc> khoaHocList = db.KhoaHocs.Where(t => t.TrangThai == "Đã duyệt").ToList();
            ViewBag.GiamGiaListView = giamGiaList;
            ViewBag.KhoaHocListView = khoaHocList;
            var khoahoc_giamgia = new KhoaHoc_GiamGia();
            return View(khoahoc_giamgia);
        }
        [HttpPost]
        public ActionResult ThemKhoaHoc_GiamGia(KhoaHoc_GiamGia khgg)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var khoahoc_giamgia = new KhoaHoc_GiamGia
            {
                MaKhoaHoc = khgg.MaKhoaHoc,
                MaGiamGia = khgg.MaGiamGia
            };
            KhoaHoc_GiamGia findKhoaHocGiamGia = db.KhoaHoc_GiamGias.FirstOrDefault(t => t.MaGiamGia == khoahoc_giamgia.MaGiamGia && t.MaKhoaHoc == khoahoc_giamgia.MaKhoaHoc);
            if (findKhoaHocGiamGia != null)
            {
                TempData["ErrorMessage"] = "Khóa học đã tồn tại mã giảm giá này!";
                return RedirectToAction("ThemKhoaHoc_GiamGia", new { maGG = khoahoc_giamgia.MaGiamGia });
            }
            db.KhoaHoc_GiamGias.InsertOnSubmit(khoahoc_giamgia);
            db.SubmitChanges();
            TempData["SuccessMessage"] = "Thêm khóa học thành công!";
            return RedirectToAction("ChiTietGiamGia", new { maGG = khoahoc_giamgia.MaGiamGia });
        }


        public ActionResult QuanLyThanhToan()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
                    // Truy vấn dữ liệu trong phạm vi giao dịch
            var thanhToans = db.ThanhToans.OrderByDescending(t => t.NgayThanhToan).ToList();
            return View(thanhToans);  
        }

        public ActionResult ChiTietThanhToan(string matt)
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap");
            }
            var sql = @"SELECT 
                tt.MaThanhToan,
                tt.MaHocVien,
                tt.SoTien AS TongSoTienThanhToan,
                tt.NgayThanhToan,
                tt.TrangThai AS TrangThaiThanhToan,
                ctt.MaChiTiet,
                ctt.SoTien AS SoTienChiTiet,
                ctt.MaGiamGia,
	            kh.TenKhoaHoc,
                hv.HoTen
            FROM 
                ThanhToan tt
            JOIN 
                ChiTietThanhToan ctt ON tt.MaThanhToan = ctt.MaThanhToan
            JOIN 
	            DangKy dk ON ctt.MaChiTiet = dk.MaChiTiet
            JOIN
	            KhoaHoc kh on dk.MaKhoaHoc = kh.MaKhoaHoc
            JOIN 
	            HocVien hv on tt.MaHocVien = hv.MaHocVien
            WHERE 
                tt.MaThanhToan = '" + matt + "'";

            ViewBag.DSGiamGia = db.GiamGias.ToList();
            var result = db.ExecuteQuery<QuanTriVienModel.ChiTietThanhToanModel>(sql).ToList();
            return View(result);
        }
    }
}
