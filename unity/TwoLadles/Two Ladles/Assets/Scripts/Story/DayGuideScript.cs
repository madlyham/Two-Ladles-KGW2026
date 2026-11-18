using System.Collections.Generic;

namespace TwoLadles.Story
{
    /// <summary>One step of the start-of-day "new dish" guide.</summary>
    [System.Serializable]
    public class DayGuideStep
    {
        public string titleEN;
        public string textEN;
        public string textVI;
        public string textKO;
        /// <summary>"linh", "jiwon" or "both".</summary>
        public string chef;
        /// <summary>Station the guide arrow points at (may be empty).</summary>
        public string stationId;
        /// <summary>Item id that completes this step, or "serve" for a delivered dish.</summary>
        public string completionItemId;

        public DayGuideStep(string titleEN, string textEN, string textVI, string textKO,
                            string chef, string stationId, string completionItemId)
        {
            this.titleEN = titleEN;
            this.textEN = textEN;
            this.textVI = textVI;
            this.textKO = textKO;
            this.chef = chef;
            this.stationId = stationId;
            this.completionItemId = completionItemId;
        }
    }

    /// <summary>
    /// Short guided walkthrough of each campaign day's new dish.
    /// Host-authoritative: the host advances steps and broadcasts them to the guest.
    /// </summary>
    public static class DayGuideScript
    {
        /// <summary>Solo mode: a step auto-advances after this many seconds (bot does the work).</summary>
        public const float BOT_STEP_TIMEOUT = 45f;

        private static DayGuideStep Serve(string dish)
        {
            return new DayGuideStep(
                "Serve it!",
                $"Bring the finished {dish} to the Serving Window.",
                $"Mang {dish} đã xong ra Quầy Phục Vụ.",
                $"완성된 {dish}을(를) 서빙 창구로 가져가세요.",
                "both", "serving_station", "serve");
        }

        public static List<DayGuideStep> GetSteps(int day)
        {
            switch (day)
            {
                case 1:
                    return new List<DayGuideStep>
                    {
                        new DayGuideStep("Prep the rice cakes",
                            "Grab tteok and prep it into raw tteokbokki at the West counter.",
                            "Lấy bánh gạo và sơ chế thành tteokbokki sống ở quầy phía Tây.",
                            "떡을 가져와 서쪽 조리대에서 떡볶이 재료로 준비하세요.",
                            "linh", "counter_west", "tteokbokki_raw"),
                        new DayGuideStep("Simmer in gochujang",
                            "Put the raw tteokbokki in the Simmer Wok until it is cooked.",
                            "Cho tteokbokki sống vào chảo om đến khi chín.",
                            "떡볶이 재료를 웍에 넣고 익을 때까지 졸이세요.",
                            "jiwon", "simmer_wok", "tteokbokki"),
                        Serve("Tteokbokki (in a Bowl)"),
                    };
                case 2:
                    return new List<DayGuideStep>
                    {
                        new DayGuideStep("Cook the flour cake",
                            "Take flour from the West shelf and cook it on the West stove.",
                            "Lấy bột ở kệ phía Tây và nấu trên bếp phía Tây.",
                            "서쪽 선반에서 밀가루를 꺼내 서쪽 화구에서 익히세요.",
                            "linh", "noodle_blancher", "cooked_flour"),
                        new DayGuideStep("Add the egg",
                            "Combine the cooked flour cake with an egg.",
                            "Kết hợp bánh bột đã nấu với trứng.",
                            "익힌 떡에 달걀을 합치세요.",
                            "both", "noodle_blancher", "bot_chien_batter"),
                        new DayGuideStep("Pan-fry it",
                            "Cook the flour cake + egg on the stove to make Bột Chiên.",
                            "Chiên bánh bột + trứng trên bếp để làm Bột Chiên.",
                            "떡+달걀을 화구에서 부쳐 보트 찌엔을 만드세요.",
                            "linh", "noodle_blancher", "bot_chien"),
                        Serve("Bột Chiên (on a Plate)"),
                    };
                case 3:
                    return new List<DayGuideStep>
                    {
                        new DayGuideStep("Prep the BBQ meat",
                            "Chop raw meat into Korean BBQ meat at the East prep board.",
                            "Thái thịt sống thành thịt BBQ Hàn ở thớt phía Đông.",
                            "동쪽 도마에서 생고기를 손질해 바비큐 고기로 만드세요.",
                            "jiwon", "prep_east", "korean_bbq_raw"),
                        new DayGuideStep("Grill the beef",
                            "Grill the BBQ meat on the Grill Pan.",
                            "Nướng thịt BBQ trên chảo nướng.",
                            "바비큐 고기를 그릴 팬에 구우세요.",
                            "jiwon", "grill_pan", "bbq_beef"),
                        new DayGuideStep("Cook the rice",
                            "Cook a rice portion on the West stove.",
                            "Nấu phần gạo trên bếp phía Tây.",
                            "서쪽 화구에서 쌀을 익히세요.",
                            "linh", "noodle_blancher", "cooked_rice"),
                        new DayGuideStep("Beef on rice",
                            "Combine the grilled beef with the cooked rice.",
                            "Kết hợp thịt nướng với cơm.",
                            "구운 고기와 밥을 합치세요.",
                            "both", "counter_west", "korean_bbq_rice"),
                        Serve("Korean BBQ on Rice (on a Plate)"),
                    };
                case 4:
                    return new List<DayGuideStep>
                    {
                        new DayGuideStep("Grill the beef",
                            "Grill BBQ meat on the Grill Pan.",
                            "Nướng thịt BBQ trên chảo nướng.",
                            "바비큐 고기를 그릴 팬에 구우세요.",
                            "jiwon", "grill_pan", "bbq_beef"),
                        new DayGuideStep("Cook the rice",
                            "Cook a rice portion on the West stove.",
                            "Nấu phần gạo trên bếp phía Tây.",
                            "서쪽 화구에서 쌀을 익히세요.",
                            "linh", "noodle_blancher", "cooked_rice"),
                        new DayGuideStep("Build Cơm Tấm",
                            "Combine rice, grilled beef and an egg (any order).",
                            "Kết hợp cơm, thịt nướng và trứng (thứ tự nào cũng được).",
                            "밥, 구운 고기, 달걀을 합치세요 (순서 상관없음).",
                            "both", "counter_west", "viet_broken_rice"),
                        Serve("Cơm Tấm (on a Plate)"),
                    };
                case 5:
                    return new List<DayGuideStep>
                    {
                        new DayGuideStep("Pickle the veg",
                            "Prep pickled daikon & carrot at the East prep board.",
                            "Làm đồ chua củ cải & cà rốt ở thớt phía Đông.",
                            "동쪽 도마에서 무·당근 절임을 준비하세요.",
                            "jiwon", "prep_east", "pickled_veg"),
                        new DayGuideStep("Sear the bulgogi",
                            "Sear bulgogi beef on the Grill Pan.",
                            "Áp chảo thịt bulgogi trên chảo nướng.",
                            "불고기를 그릴 팬에 구우세요.",
                            "jiwon", "grill_pan", "bulgogi_seared"),
                        new DayGuideStep("Build the Bánh Mì",
                            "Combine pickled veg, bulgogi and a baguette.",
                            "Kết hợp đồ chua, bulgogi và bánh mì.",
                            "절임, 불고기, 바게트를 합치세요.",
                            "both", "counter_east", "bulgogi_banh_mi"),
                        Serve("Bulgogi Bánh Mì (on a Plate)"),
                    };
                default:
                    return new List<DayGuideStep>();
            }
        }
    }
}
