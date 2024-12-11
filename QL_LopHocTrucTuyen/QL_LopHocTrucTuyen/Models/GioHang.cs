using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_LopHocTrucTuyen.Models
{
    public class ChiTietSP
    {
        public KhoaHoc kh { get; set; }
        public GiamGia gg { get; set; }

        public ChiTietSP(KhoaHoc k)
        {
            kh = k;
            gg = null;
        }

        public ChiTietSP(KhoaHoc k, GiamGia g)
        {
            kh = k;
            gg = g;
        }

        public double Tien()
        {
            double giam = 0;
            if (gg != null) giam = (double.Parse(kh.Gia.ToString()) * double.Parse(gg.PhanTramGiam.ToString()) / 100);

            return double.Parse(kh.Gia.ToString()) - giam;
        }
    }


    public class GioHang
    {
        private DataClasses1DataContext db = new DataClasses1DataContext();

        // Danh sách chi tiết sản phẩm (bao gồm khóa học và giảm giá, nếu có)
        public List<ChiTietSP> ct { get; set; }

        // Constructor khởi tạo giỏ hàng
        public GioHang()
        {
            ct = new List<ChiTietSP>(); // Khởi tạo danh sách chi tiết sản phẩm
        }

        // Thêm khóa học vào giỏ hàng (chỉ lưu trong bộ nhớ, không gọi db)
        public void ThemVaoGio(KhoaHoc khoaHoc)
        {
            if (!ct.Where(t => t.kh.MaKhoaHoc == khoaHoc.MaKhoaHoc).Any())
            {
                ChiTietSP s = new ChiTietSP(khoaHoc);
                ct.Add(s);
            }
        }


        public double TongTien()
        {
            double tongTien = 0;

            // Duyệt qua danh sách các khóa học trong giỏ
            foreach (var sp in ct)
            {
                tongTien += sp.Tien();
            }

            return tongTien; // Trả về tổng tiền sau giảm
        }

        public double TongTienChuaGiam()
        {
            double tongTien = 0;

            // Duyệt qua danh sách các khóa học trong giỏ
            foreach (var sp in ct)
            {
                tongTien += double.Parse(sp.kh.Gia.ToString());
            }

            return tongTien; // Trả về tổng tiền sau giảm
        }

    }
}