using Team5.Data;

namespace Team5.UI
{
    // クリア画面の職業ごとの文（ResultView と ClearResultPresentation で使う）。
    // 「〇〇になるはずだった。」に続く一文と、村長の一言。職業の完成品・roles.md の設定に合わせる
    public static class ClearResultText
    {
        // 物語の 2 行目
        public static string Story(RoleDefinition role)
        {
            switch (role != null ? role.roleId : null)
            {
                case "ramen_chef": return "けれど、いま手にしているのは、自分で作った一杯だ。";
                case "band_man": return "けれど、いま鳴らしているのは、自分で作った曲だ。";
                case "librarian": return "けれど、いま並べているのは、自分で直した本だ。";
                case "progamer": return "けれど、いま握っているのは、世界大会への招待状だ。";
                default: return "けれど、いまここにいるのは、自分で選んだわたしだ。";
            }
        }

        // 村長の一言（「」の中）。改行の位置は \n で指定し、1 行で出す画面では取り除く
        public static string ChiefLine(RoleDefinition role)
        {
            switch (role != null ? role.roleId : null)
            {
                case "ramen_chef": return "……まあ、一杯くらいは、\nいただこうか。";
                case "band_man": return "……まあ、一曲くらいは、\n聴いてやろうか。";
                case "librarian": return "……静かな場所が、\nひとつくらいあってもよいか。";
                case "progamer": return "……畑より、その盤の上が\n性に合っておったか。";
                default: return "……それが、\nお前の選んだ道か。";
            }
        }

        public static string ChiefLineOneLine(RoleDefinition role) => ChiefLine(role).Replace("\n", "");
    }
}
