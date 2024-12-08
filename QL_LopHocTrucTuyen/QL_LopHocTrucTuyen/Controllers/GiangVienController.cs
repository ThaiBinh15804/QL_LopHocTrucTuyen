using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using QL_LopHocTrucTuyen.Models;
using System.Data.Entity;
using System.Web.Security;

namespace QL_LopHocTrucTuyen.Controllers
{
    [Authorize]
    public class GiangVienController : Controller
    {
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult Index()
        {
            GiangVien gv = (GiangVien)Session["user"];
            return View(data.KhoaHocs.Where(t => t.MaGiangVien == gv.MaGiangVien).Take(5).ToList());
        }

        public string TaoMa(string macu)
        {
            string prefix = macu.Substring(0, 2);
            int numericPart = int.Parse(macu.Substring(2));
            string mamoi = prefix + (numericPart + 1).ToString("D3");

            return mamoi;
        }

        public ActionResult HienThiKhoaHoc()
        {
            GiangVien gv = (GiangVien)Session["user"];
            return View(data.KhoaHocs.Where(t => t.MaGiangVien == gv.MaGiangVien).ToList());
        }

        public ActionResult KhoaHoc(string makh)
        {
            KhoaHoc kh = data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh);
            return View(kh);
        }

        public ActionResult XuLyDieuHuong(string makh, string page)
        {
            TempData["DieuHuong"] = page;
            return RedirectToAction("KhoaHoc", new { makh = makh });
        }

        public ActionResult BaiHoc(string makh)
        {
            KhoaHoc kh = data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh);
            return PartialView(kh);
        }

        public ActionResult BaoCao(string makh)
        {
            return PartialView(data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh));
        }

        public ActionResult BaiTap(string makh)
        {
            return PartialView(data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh));
        }

        public ActionResult HocVien(string makh)
        {
            List<HocVien> lst = data.HocViens.Where(hv => hv.ThanhToans.
                Where(t => t.ChiTietThanhToans.
                    Where(ct => ct.DangKies.Where(dk => dk.MaKhoaHoc == makh).Any()).Any()).Any()).ToList();

            ViewBag.MaKhoaHoc = makh;
            ViewBag.TongBaiGiang = data.BaiGiangs.Where(t => t.Chuong.MaKhoaHoc.ToString() == makh).Count();

            return PartialView(lst);
        }

        public ActionResult ThietLap(string makh)
        {
            ViewBag.LoaiKhoaHocs = data.LoaiKhoaHocs.ToList();
            return PartialView(data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh));
        }

        [AllowAnonymous]
        public ActionResult DangNhap()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult XuLyDangNhap(FormCollection c)
        {
            string tenDN = c["username"];
            string mk = c["password"];

            NguoiDung user = data.NguoiDungs.FirstOrDefault(t => t.TenDangNhap == tenDN && t.MatKhau == mk);

            if (user != null)
            {
                GiangVien gv = data.GiangViens.FirstOrDefault(t => t.MaGiangVien == user.MaNguoiDung);
                Session["user"] = gv;
                TempData["ThongBao"] = "Đăng nhập thành công";
                FormsAuthentication.SetAuthCookie(user.TenDangNhap, false);
            }
            else
            {
                TempData["ThongBao"] = "Đăng nhập thất bại";
            }

            return RedirectToAction("Index");
        }

        public ActionResult TaoKH_1()
        {
            KhoaHoc kh = new KhoaHoc();
            ViewBag.LoaiKhoaHocs = data.LoaiKhoaHocs.ToList();
            return PartialView(kh);
        }

        [HttpPost]
        public ActionResult XuLyTaoKH_1(KhoaHoc kh)
        {
            if (ModelState.IsValid)
            {
                GiangVien gv = (GiangVien)Session["user"];

                kh.MaKhoaHoc = TaoMa(data.KhoaHocs.OrderByDescending(t => t.MaKhoaHoc).FirstOrDefault().MaKhoaHoc);
                kh.MaGiangVien = gv.MaGiangVien;
                kh.TrangThai = "Chưa duyệt";
                data.KhoaHocs.InsertOnSubmit(kh);
                data.SubmitChanges();

                TempData["ThongBao"] = "Thêm khoá học thành công";
            }
            else
            {
                TempData["ThongBao"] = "Thêm khoá học không thành công";

            }
            return RedirectToAction("HienThiKhoaHoc");
        }

        public ActionResult XoaKhoaHoc(string makh)
        {
            KhoaHoc kh = data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh);

            kh.TrangThai = "Ngừng hoạt động";

            data.SubmitChanges();

            return RedirectToAction("Index");
        }

        public ActionResult TestUploadVideo()
        {
            return View();
        }

        [HttpPost]
        public ActionResult XuLy_TestUploadVideo(HttpPostedFileBase video, string url, string optionUpload)
        {
            if (optionUpload == "file")
            {
                if (video != null)
                {
                    string filename = video.FileName;

                    string duongdan = Path.Combine(Server.MapPath("~/Content/Video"), filename);

                    video.SaveAs(duongdan);

                    TempData["ThongBao"] = "Upload file thành công";
                }
                else
                {
                    TempData["ThongBao"] = "Upload file thất bại";
                }
            }
            else
            {
                if (url != "")
                {
                    string s = "Upload url thành công" + url;
                    TempData["ThongBao"] = url;
                }
                else
                    TempData["ThongBao"] = "Upload url không thành công";
            }

            return RedirectToAction("Index", "GiangVien");


        }

        public ActionResult Test()
        {
            KhoaHoc kh = data.KhoaHocs
                .Include(k => k.Chuongs.Select(c => c.BaiGiangs)) // Eager load các chương và bài giảng
                .FirstOrDefault(t => t.MaKhoaHoc == "KH001");

            kh.Chuongs.ToList();

            return View(kh);
        }

        [HttpPost]
        public ActionResult TaoChuong(string makh, FormCollection c)
        {
            Chuong ch = new Chuong();
            string ma = data.Chuongs.OrderByDescending(t => t.MaChuong).FirstOrDefault().MaChuong;

            ch.MaChuong = TaoMa(ma);
            ch.TenChuong = c["TenChuong"];
            ch.MaKhoaHoc = makh;
            ch.MoTa = c["MoTa"];
            ch.ThuTu = int.Parse(c["ThuTu"]) + 1;

            data.Chuongs.InsertOnSubmit(ch);
            data.SubmitChanges();

            return Redirect(HttpContext.Request.UrlReferrer.ToString());
        }

        public ActionResult SuaChuong(string machuong)
        {
            return PartialView(data.Chuongs.FirstOrDefault(t => t.MaChuong.ToString() == machuong));
        }

        [HttpPost]
        public ActionResult XuLySuaChuong(Chuong ch)
        {
            Chuong cu = data.Chuongs.FirstOrDefault(t => t.MaChuong == ch.MaChuong);
            //cu.TenChuong = ch.TenChuong;
            //cu.MoTa = ch.MoTa;
            //cu.ThuTu = ch.ThuTu;
            //UpdateModel(cu);
            UpdateModel(cu, new[] { "TenChuong", "MoTa", "ThuTu" });
            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = ch.MaKhoaHoc });
        }

        public ActionResult TaoBaiGiang(string machuong, FormCollection c)
        {
            Chuong ch = data.Chuongs.FirstOrDefault(t => t.MaChuong.ToString() == machuong);

            BaiGiang bg = new BaiGiang();

            bg.MaBaiGiang = TaoMa(data.BaiGiangs.OrderByDescending(t => t.MaBaiGiang).FirstOrDefault().MaBaiGiang);
            bg.TenBaiGiang = c["TenBaiGiang"];
            bg.MaChuong = ch.MaChuong;
            bg.ThuTu = int.Parse(c["ThuTu"]) + 1;
            data.BaiGiangs.InsertOnSubmit(bg);
            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = ch.MaKhoaHoc });
        }

        public ActionResult XoaChuong(string machuong)
        {
            Chuong ch = data.Chuongs.FirstOrDefault(t => t.MaChuong.ToString() == machuong);

            return PartialView(ch);
        }

        public ActionResult XuLyXoaChuong(string machuong)
        {
            Chuong ch = data.Chuongs.FirstOrDefault(t => t.MaChuong.ToString() == machuong);

            data.BaiGiangs.DeleteAllOnSubmit(ch.BaiGiangs);
            data.BaiTaps.DeleteAllOnSubmit(ch.BaiTaps);

            data.Chuongs.DeleteOnSubmit(ch);

            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = ch.MaKhoaHoc });
        }

        public ActionResult SuaBaiGiang(string mabg)
        {
            BaiGiang bg = data.BaiGiangs.FirstOrDefault(t => t.MaBaiGiang.ToString() == mabg);
            return View(bg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult XuLySuaBaiGiang(BaiGiang bg, HttpPostedFileBase FileVideo)
        {
            BaiGiang cu = data.BaiGiangs.FirstOrDefault(t => t.MaBaiGiang == bg.MaBaiGiang);

            if (bg.URL != null && cu.URL != bg.URL)
            {
                cu.URL = bg.URL;
                cu.FileVideo = null;
            }

            if (FileVideo != null && FileVideo.ContentLength > 0 && cu.FileVideo != bg.FileVideo)
            {
                // Xử lý tải lên file video
                string fileName = Path.GetFileName(FileVideo.FileName);
                string duongdan = Path.Combine(Server.MapPath("~/Content/Video"), fileName);
                FileVideo.SaveAs(duongdan);

                cu.FileVideo = fileName;
                cu.URL = null;
            }

            // Cập nhật chỉ những thuộc tính có sự thay đổi
            if (cu.NoiDung != bg.NoiDung)
            {
                cu.NoiDung = bg.NoiDung;
            }

            if (cu.ThuTu != bg.ThuTu)
            {
                cu.ThuTu = bg.ThuTu;
            }


            // Lưu thay đổi
            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = cu.Chuong.MaKhoaHoc });
        }


        public ActionResult XemBaiGiang(string mabg)
        {
            return View(data.BaiGiangs.FirstOrDefault(t => t.MaBaiGiang.ToString() == mabg));
        }

        public ActionResult XoaBaiGiang(string mabg)
        {
            return PartialView(data.BaiGiangs.FirstOrDefault(t => t.MaBaiGiang.ToString() == mabg));
        }

        public ActionResult XuLyXoaBaiGiang(string mabg)
        {
            BaiGiang bg = data.BaiGiangs.FirstOrDefault(t => t.MaBaiGiang.ToString() == mabg);
            data.BaiGiangs.DeleteOnSubmit(bg);
            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = bg.Chuong.MaKhoaHoc });
        }

        public ActionResult XemBinhLuan(string mabg)
        {
            List<BinhLuan> lst = data.BinhLuans.Where(t => t.MaBaiGiang.ToString() == mabg).ToList();
            return PartialView(lst);
        }

        public ActionResult ThemBinhLuan(string mabg, FormCollection c)
        {
            GiangVien gv = (GiangVien)Session["user"];
            BinhLuan a = new BinhLuan();

            a.MaBinhLuan = TaoMa(data.BinhLuans.OrderByDescending(t => t.MaBinhLuan).FirstOrDefault().MaBinhLuan);
            a.MaBaiGiang = mabg;
            a.NoiDung = c["binhluanmoi"];
            a.MaNguoiDung = gv.MaGiangVien;
            a.NgayTao = DateTime.Now;
            a.MaBinhLuanCha = c["binhluancha"];


            data.BinhLuans.InsertOnSubmit(a);
            data.SubmitChanges();

            return RedirectToAction("XemBaiGiang", new { mabg = mabg });
        }

        public ActionResult SuaKhoaHoc(string makh, HttpPostedFileBase imageUpload, FormCollection c)
        {
            KhoaHoc cu = data.KhoaHocs.FirstOrDefault(t => t.MaKhoaHoc.ToString() == makh);

            if (imageUpload != null && imageUpload.ContentLength > 0)
            {
                // Lưu ảnh bìa
                string fileName = Path.GetFileName(imageUpload.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/HinhAnh/KhoaHoc"), fileName);
                imageUpload.SaveAs(path);
                cu.AnhBia = fileName;
            }

            cu.TenKhoaHoc = c["TenKhoaHoc"];
            cu.MoTa = c["MoTa"];
            cu.MaLoaiKhoaHoc = c["LoaiKhoaHoc"];

            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = makh });
        }

        public ActionResult ChamDiem(string mabt, string madk)
        {
            return View(data.DangKy_BaiTaps.FirstOrDefault(t => t.MaBaiTap.ToString() == mabt && t.MaDangKy.ToString() == madk));
        }

        public ActionResult XuLyChamDiem(DangKy_BaiTap dk)
        {
            DangKy_BaiTap cu = data.DangKy_BaiTaps.FirstOrDefault(t => t.MaDangKy == dk.MaDangKy && t.MaBaiTap == dk.MaBaiTap);

            if (cu.Diem != dk.Diem)
            {
                cu.Diem = dk.Diem;
                cu.TrangThai = "Đã chấm";
            }

            data.SubmitChanges();

            TempData["DieuHuong"] = "BaiTap";
            return RedirectToAction("KhoaHoc", new { makh = cu.BaiTap.Chuong.MaKhoaHoc.ToString() });
        }

        public ActionResult TaoBaiTap(string machuong, FormCollection c)
        {
            BaiTap bt = new BaiTap();

            bt.MaBaiTap = TaoMa(data.BaiTaps.OrderByDescending(t => t.MaBaiTap).FirstOrDefault().MaBaiTap);
            bt.TenBaiTap = c["TenBaiTap"];
            bt.ThuTu = int.Parse(c["ThuTu"]) + 1;
            bt.MaChuong = machuong;

            Chuong ch = data.Chuongs.FirstOrDefault(t => t.MaChuong.ToString() == machuong);

            data.BaiTaps.InsertOnSubmit(bt);
            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = ch.MaKhoaHoc });
        }

        public ActionResult XemBaiTap(string mabt)
        {
            return View(data.BaiTaps.FirstOrDefault(t => t.MaBaiTap.ToString() == mabt));
        }

        public ActionResult SuaBaiTap(string mabt)
        {
            BaiTap bt = data.BaiTaps.FirstOrDefault(t => t.MaBaiTap.ToString() == mabt);
            return View(bt);
        }

        public ActionResult XuLySuaBaiTap(BaiTap b, HttpPostedFileBase UploadedFile)
        {
            BaiTap bt = data.BaiTaps.FirstOrDefault(t => t.MaBaiTap == b.MaBaiTap);

            if (bt.TenBaiTap != b.TenBaiTap)
            {
                bt.TenBaiTap = b.TenBaiTap;
            }

            if (bt.MoTa != b.MoTa)
            {
                bt.MoTa = b.MoTa;
            }

            if (bt.ThuTu != b.ThuTu)
            {
                bt.ThuTu = b.ThuTu;
            }

            if (UploadedFile != null && UploadedFile.ContentLength > 0)
            {
                // Xử lý tải lên file video
                string fileName = Path.GetFileName(UploadedFile.FileName);
                string duongdan = Path.Combine(Server.MapPath("~/Content/File/BaiTap"), fileName);
                UploadedFile.SaveAs(duongdan);

                bt.FileUpload = fileName;
            }

            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = bt.Chuong.MaKhoaHoc.ToString() });
        }

        public ActionResult XoaBaiTap(string mabt)
        {
            return PartialView(data.BaiTaps.FirstOrDefault(t => t.MaBaiTap.ToString() == mabt));
        }

        public ActionResult XuLyXoaBaiTap(FormCollection c)
        {
            BaiTap bt = data.BaiTaps.FirstOrDefault(t => t.MaBaiTap.ToString() == c["mabt"]);
            data.BaiTaps.DeleteOnSubmit(bt);

            data.SubmitChanges();

            return RedirectToAction("KhoaHoc", new { makh = bt.Chuong.MaKhoaHoc });
        }

        public ActionResult TimKiem(FormCollection c)
        {
            string tk = c["TenKhoaHoc"].ToLower();
            List<KhoaHoc> lst = data.KhoaHocs.Where(t => t.TenKhoaHoc.ToLower().Contains(tk)).ToList();

            TempData["lst"] = lst;

            return RedirectToAction("BoLoc");
        }

        public ActionResult BoLoc()
        {
            List<KhoaHoc> lst = (List<KhoaHoc>)TempData["lst"];
            return View(lst);
        }

        public ActionResult LoaiKhoaHocList()
        {
            return View(data.LoaiKhoaHocs);
        }

        public ActionResult XuLyBoLoc(FormCollection c)
        {
            List<KhoaHoc> lst = data.KhoaHocs.ToList();

            if (!String.IsNullOrEmpty(c["LoaiKhoaHoc"]))
            {
                lst = lst.Where(t => t.MaLoaiKhoaHoc.ToString() == c["LoaiKhoaHoc"]).ToList();
            }

            if (c["TrangThai"] != null)
            {
                lst = lst.Where(t => t.TrangThai == c["TrangThai"]).ToList();
            }

            if (c["TenKhoaHoc"] != null)
            {
                string tenkh = c["TenKhoaHoc"].ToLower();
                lst = lst.Where(t => t.TenKhoaHoc.ToLower().Contains(tenkh)).ToList();
            }

            if (!string.IsNullOrEmpty(c["NgayBD"]))
            {
                DateTime ngaybd;
                if (DateTime.TryParse(c["NgayBD"], out ngaybd))
                {
                    lst = lst.Where(t => t.NgayBatDau >= ngaybd).ToList();
                }
                else
                {
                    ModelState.AddModelError("NgayBD", "Ngày bắt đầu không hợp lệ.");
                }
            }

            if (!string.IsNullOrEmpty(c["NgayKT"]))
            {
                DateTime ngaykt;
                if (DateTime.TryParse(c["NgayKT"], out ngaykt))
                {
                    lst = lst.Where(t => t.NgayBatDau <= ngaykt).ToList();
                }
                else
                {
                    ModelState.AddModelError("NgayKT", "Ngày kết thúc không hợp lệ.");
                }
            }


            TempData["lst"] = lst;
            return RedirectToAction("BoLoc", lst);
        }

        public ActionResult ThongTinNguoiDung()
        {
            GiangVien gv = (GiangVien)Session["user"];
            return View(gv);
        }

        [HttpPost]
        public ActionResult SuaThongTinNguoiDung(FormCollection c, HttpPostedFileBase anh)
        {
            GiangVien gv = (GiangVien)Session["user"];
            GiangVien cu = data.GiangViens.FirstOrDefault(t => t.MaGiangVien == gv.MaGiangVien);

            cu.HoTen = c["hoten"];
            cu.ChuyenNganh = c["chuyennganh"];
            cu.SoDienThoai = c["sdt"];
            cu.DiaChi = c["diachi"];
            cu.NgayGiaNhap = DateTime.Parse(c["ngaygianhap"]);

            if (anh != null)
            {
                string fileName = Path.GetFileName(anh.FileName);
                string duongdan = Path.Combine(Server.MapPath("~/Content/HinhAnh/Avatar"), fileName);
                anh.SaveAs(duongdan);
                cu.NguoiDung.AnhDaiDien = fileName;
            }

            Session["user"] = cu;

            data.SubmitChanges();

            return RedirectToAction("ThongTinNguoiDung");
        }
    }
}
