using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using QL_LopHocTrucTuyen.Models;

namespace QL_LopHocTrucTuyen.Models
{
    public partial class BinhLuan
    {
        DataClasses1DataContext db = new DataClasses1DataContext();
        public List<BinhLuan> TatCaBinhLuan
        {
            get
            {
                return db.Chuongs
                    .SelectMany(chuong => chuong.BaiGiangs)
                    .SelectMany(baiGiang => baiGiang.BinhLuans)
                    .ToList();
            }
        }

    }
}