using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_LopHocTrucTuyen.Models
{
    public class QuanTriVienModel
    {
        public class QuanTriVienPagedList
        {
            public List<QuanTriVien> AdminList { get; set; } // Danh sách quản trị viên
            public int CurrentPage { get; set; }  // Trang hiện tại
            public int TotalPages { get; set; }   // Tổng số trang
            public int PageSize { get; set; }     // Số lượng bản ghi mỗi trang
            public string SearchQuery { get; set; }
        }
        public class GiangVienPagedList
        {
            public List<GiangVien> InstructorList { get; set; } // Danh sách quản trị viên
            public int CurrentPage { get; set; }  // Trang hiện tại
            public int TotalPages { get; set; }   // Tổng số trang
            public int PageSize { get; set; }     // Số lượng bản ghi mỗi trang
            public string SearchQuery { get; set; }
        }
        public class HocVienPagedList
        {
            public List<HocVien> StudentList { get; set; } // Danh sách quản trị viên
            public int CurrentPage { get; set; }  // Trang hiện tại
            public int TotalPages { get; set; }   // Tổng số trang
            public int PageSize { get; set; }     // Số lượng bản ghi mỗi trang
            public string SearchQuery { get; set; }
        }
        public class LoaiKhoaHocPagedList
        {
            public List<LoaiKhoaHoc> LoaiKhoaHoc { get; set; } // Danh sách loai khoa hoc
            public int CurrentPage { get; set; }  // Trang hiện tại
            public int TotalPages { get; set; }   // Tổng số trang
            public int PageSize { get; set; }     // Số lượng bản ghi mỗi trang
            public string SearchQuery { get; set; }
        }
        public class ThongKeSoTuoi
        {
            public int Tuoi { get; set; }
            public int SoLuong { get; set; }
        }

        public class SoluongDangKyTheoThangResult
        {
            public string ThangNam { get; set; }
            public int SoLuongDangKy { get; set; }
        }
        public class DoanhThuTheoThang
        {
            public string ThangNam { get; set; }
            public decimal DoanhThu { get; set; }
        }

        public class KhoaHocTheoLoai
        {
            public string TenLoaiKhoaHoc { get; set; }
            public int SoKhoaHoc { get; set; }
        }

        public class GiamGiaPagedList
        {
            public List<GiamGia> giamGiaList { get; set; } // Danh sách loai khoa hoc
            public int CurrentPage { get; set; }  // Trang hiện tại
            public int TotalPages { get; set; }   // Tổng số trang
            public int PageSize { get; set; }     // Số lượng bản ghi mỗi trang
            public string SearchQuery { get; set; }
        }

        public class ChiTietThanhToanModel
        {
            // Thông tin thanh toán
            public string MaThanhToan { get; set; } // Mã thanh toán
            public string MaHocVien { get; set; } // Mã học viên
            public decimal TongSoTienThanhToan { get; set; } // Tổng số tiền thanh toán
            public DateTime? NgayThanhToan { get; set; } // Ngày thanh toán
            public string TrangThaiThanhToan { get; set; } // Trạng thái thanh toán

            // Thông tin chi tiết thanh toán
            public string MaChiTiet { get; set; } // Mã chi tiết thanh toán
            public decimal SoTienChiTiet { get; set; } // Số tiền trong chi tiết thanh toán
            public string MaGiamGia { get; set; } // Mã giảm giá (có thể null)
            public string TenKhoaHoc { get; set; } // Ngày thực hiện thanh toán chi tiết
            public string HoTen { get; set; } // Ngày thực hiện thanh toán chi tiết
        }

    }
}