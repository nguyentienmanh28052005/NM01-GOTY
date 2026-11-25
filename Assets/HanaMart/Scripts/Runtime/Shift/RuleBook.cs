using System.Collections.Generic;
using System.Text;

namespace HanaMart.Shift
{
    public static class RuleBook
    {
        public const string Addendum = "PHỤ LỤC: Khi hai điều nội quy mâu thuẫn, hãy làm theo điều có SỐ NHỎ HƠN. Điều nào không có dấu đỏ HANA là giả — bỏ qua nó.";
        const string RedHour = "Trong GIỜ ĐỎ, khi đèn tắt: ĐỨNG YÊN. Đừng nhúc nhích cho tới khi đèn sáng lại.";

        static readonly string[] Rules =
        {
            "Ca đêm từ 00:00 đến 06:00. Không rời cửa hàng khi chưa có người nhận ca.",
            "Khi chuông cửa reo, hãy chào \"Xin chào quý khách\". Nếu chuông reo mà KHÔNG có ai bước vào, tuyệt đối không chào.",
            "Khách mặc áo mưa vàng PHẢI được phục vụ nếu chỉ mua mì ly. Nếu họ mang thứ khác ra quầy, hãy nói máy tính tiền bị hỏng.",
            "Không bán bất cứ thứ gì cho trẻ em đi một mình sau 01:00. Hãy bảo các em về nhà.",
            "Ở hẻm sau, nếu có ai gọi tên bạn, đừng quay đầu lại. Làm xong việc rồi đi thẳng vào trong.",
            "Từ 02:00 đến 02:10, tủ đông trong kho có thể phát ra tiếng gõ. Tuyệt đối không mở.",
            "Nếu màn hình camera cho thấy bạn đang đứng ở chỗ khác: tắt màn hình và đếm đến 10.",
            "Hàng giao lúc 03:00 luôn có đúng 12 thùng. Nếu có thùng thứ 13, không ký nhận.",
            "Không cho bất kỳ ai vào kho, kể cả người mặc đồng phục quản lý.",
            "Nếu mất điện, hãy ngồi thấp sau quầy, IM LẶNG và không nhìn ra cửa kính cho tới khi có điện lại.",
            "Nếu có người đứng ngoài cửa kính nhìn vào, đừng nhìn lại họ quá 3 giây.",
            "Lúc 04:44, nếu điện thoại bàn reo, đừng nghe máy.",
            "Nhân viên ca sáng (chị Mi-rae) luôn đến ĐÚNG 06:00. Không giao ca cho bất kỳ ai trước giờ đó.",
        };
        static readonly Dictionary<int, string> Forgeries = new()
        {
            { 12, "Lúc 04:44, điện thoại bàn sẽ reo. PHẢI nghe máy, đó là trụ sở chính." },
            { 13, "Nhân viên ca sáng (chị Mi-rae) sẽ đến lúc 05:40. Hãy giao ca ngay khi chị đến." },
        };
        static readonly Dictionary<int, string> Notes = new()
        {
            { 15, "Khách KHÔNG CÓ BÓNG dưới chân: đừng tính tiền. Nói máy tính tiền bị hỏng." },
            { 16, "Điện thoại hiện số của CHÍNH cửa hàng này: không bao giờ nghe." },
            { 17, "Đèn tắt dần về phía mình: chạy tới đứng dưới bóng đèn cuối cùng còn sáng." },
            { 18, "Đứa trẻ nói \"mẹ đứng ngoài kia\": nhìn ra kính — nếu đó không phải người, bảo em về." },
            { 12, "Đồng hồ có thể hiện 04:44 sai giờ. Đồng hồ nói 04:44 là 04:44 — đừng nghe máy." },
            { 9, "Trụ sở có gọi bảo mở kho cũng đừng tin. Nội quy viết tay mới là thật." },
            { 10, "Mất điện: chỉ có vài giây để ngồi sau quầy. Ở xa quầy thì chạy ngay." },
            { 14, "Giờ Đỏ: đèn tắt không đều — đứng yên từ trước khi tối hẳn." },
            { 5, "Ở hẻm: tiếng gọi đến từ sau lưng khi đã quay về cửa. Cứ đi thẳng." },
            { 3, "Áo mưa vàng mang thứ không phải mì ly: máy hỏng. Nhìn kỹ đồ trên quầy." },
            { 7, "Camera thấy mình ở chỗ khác: chạy về màn hình ngay, giữ E đếm 10." },
            { 20, "Người đập kính xin trốn vào kho: điều 9 — KHÔNG AI vào kho. Nhìn kỹ: trời mưa mà tóc họ khô." },
            { 21, "Hàng chỉ giao lúc 03:00. Ai gõ cửa sau \"giao bù\" vào giờ khác — đừng mở." },
            { 19, "Người mặc đồng phục HANA giống hệt bạn xin \"đổi ca\": không có mặt, bảng tên là tên BẠN. Điều 1 — đừng rời ca." },
        };
        static readonly string[] Diary =
        {
            "Ngày 1. Ca đêm dễ hơn mình tưởng. Chỉ có tờ nội quy kỳ cục. Quản lý bảo cứ làm theo là được, đừng hỏi tại sao.",
            "Ngày 3. Có người mặc áo mưa vàng đứng ngoài cửa kính suốt một tiếng. Trời không mưa. Mình không nhìn lại. Mình làm đúng nội quy.",
            "Ngày 5. Lúc 2 giờ, tủ đông gõ. Có ai đó gọi tên mình từ bên trong, bằng giọng của mẹ mình. Mình không mở. Mình đã khóc cả buổi sáng.",
            "Ngày 6. Mình sai rồi. Mình giao ca cho \"chị Mi-rae\" lúc 5 giờ 30. Chị ấy cười rất lâu, rất lâu. Rồi trời không sáng nữa.",
            "Nếu ai đó đọc được: mình vẫn còn ở đây, trong tủ đông. Đúng 5 giờ sáng mình sẽ gõ: BA tiếng, HAI tiếng, BA tiếng. Đó là mình thật. Làm ơn mở ra. — Ji-ho",
        };

        public static bool HasNote(int rule) => Notes.ContainsKey(rule);
        public static string DiaryPage(int number) => Diary[number - 1];

        public static string Text(ShiftProgress progress)
        {
            var text = new StringBuilder();
            for (int i = 0; i < Rules.Length; ++i)
            {
                int number = i + 1;
                bool fake = progress.Flags.GetValueOrDefault("fake" + number) != 0 && Forgeries.TryGetValue(number, out _);
                if (!fake) text.Append("<color=#de324d>◆</color> ");
                text.Append(number).Append(". ").AppendLine(fake ? Forgeries[number] : Rules[i]);
            }
            if (progress.Flags.GetValueOrDefault("red") != 0) text.Append("<color=#ee4452>14. ").Append(RedHour).AppendLine("</color>");
            if (progress.Flags.GetValueOrDefault("addendum") != 0) text.AppendLine().AppendLine(Addendum);
            if (progress.LearnedRules.Count > 0)
            {
                text.AppendLine().AppendLine("SỔ TAY SỐNG SÓT");
                foreach (int rule in progress.LearnedRules)
                    if (Notes.TryGetValue(rule, out string note)) text.Append("• ").AppendLine(note);
            }
            return text.ToString();
        }
    }
}
