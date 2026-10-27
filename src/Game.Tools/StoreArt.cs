namespace TowerGame.Tools;

public static class StoreArt
{
    public static void Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"icon.svg"), """
            <svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">
            <defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#193e40"/><stop offset="1" stop-color="#08191f"/></linearGradient>
            <linearGradient id="shield" x2="0" y2="1"><stop stop-color="#c2e7a3"/><stop offset="1" stop-color="#5cbda2"/></linearGradient></defs>
            <rect width="512" height="512" rx="104" fill="url(#bg)"/>
            <path d="M56 120H456M56 200H456M56 280H456M56 360H456M120 56V456M200 56V456M280 56V456M360 56V456" stroke="#265253" stroke-width="2"/>
            <path d="M256 65L414 125V256Q398 364 256 448Q114 364 98 256V125Z" fill="#102b31" stroke="url(#shield)" stroke-width="14"/>
            <path d="M175 330V206H211V170H241V125H271V170H301V206H337V330Z" fill="url(#shield)"/>
            <path d="M230 330V269Q256 240 282 269V330Z" fill="#102b31"/>
            <path d="M188 351H324" stroke="#edbb71" stroke-width="10" stroke-linecap="round"/>
            </svg>
            """);
        File.WriteAllText(Path.Combine(directory,"capsule-draft.svg"), """
            <svg xmlns="http://www.w3.org/2000/svg" width="1232" height="706" viewBox="0 0 1232 706">
            <defs><linearGradient id="b" x2="1" y2=".7"><stop stop-color="#0c2029"/><stop offset="1" stop-color="#234b44"/></linearGradient>
            <pattern id="g" width="56" height="56" patternUnits="userSpaceOnUse"><path d="M56 0H0V56" fill="none" stroke="#406557" stroke-width="1"/></pattern></defs>
            <rect width="1232" height="706" fill="url(#b)"/><rect x="590" width="642" height="706" fill="url(#g)" opacity=".6"/>
            <path d="M650 590H1150V460H730V330H1100V160H850" fill="none" stroke="#d5c69c" stroke-width="28" opacity=".27" stroke-linejoin="round"/>
            <g fill="#7ed2b1" stroke="#12363c" stroke-width="12">
            <path d="M804 219L850 193L896 219V271L850 297L804 271Z"/><path d="M974 384L1020 358L1066 384V436L1020 462L974 436Z"/>
            <path d="M764 494L810 468L856 494V546L810 572L764 546Z"/></g>
            <g stroke="#e2e8bd" stroke-width="16" stroke-linecap="round"><path d="M850 245L895 207"/><path d="M1020 410L981 370"/><path d="M810 520L862 520"/></g>
            <g fill="#e7a86a"><circle cx="1100" cy="180" r="13"/><circle cx="1100" cy="232" r="13"/><circle cx="1100" cy="284" r="13"/></g>
            <text x="80" y="320" fill="#edf0db" font-family="Noto Sans,sans-serif" font-size="116" font-weight="800" letter-spacing="6">QALQON</text>
            <path d="M84 360H440" stroke="#e5b97c" stroke-width="4"/>
            <text x="84" y="412" fill="#a8c9bb" font-family="Noto Sans,sans-serif" font-size="25" letter-spacing="7">BUILD THE ROUTE.</text>
            <text x="84" y="455" fill="#a8c9bb" font-family="Noto Sans,sans-serif" font-size="25" letter-spacing="7">HOLD THE LINE.</text>
            </svg>
            """);
    }
}
