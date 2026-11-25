using System.Collections.Generic;

namespace HanaMart.Shift
{
    public enum CheckoutChoice { Charge, RegisterBroken, SendHome, AllowStorage, DenyStorage }
    public enum CheckoutEffect { Sale, Refusal, Complain, Strike, Die }

    public static class CheckoutRules
    {
        static readonly Dictionary<string, int> Prices = new()
        {
            { "ramenCup", 1500 }, { "ramenBag", 4200 }, { "chips", 1800 }, { "bottle", 1200 }, { "can", 1400 },
            { "banana", 1700 }, { "soju", 1950 }, { "kimbap", 1300 }, { "lunch", 4900 }, { "cig", 4500 }, { "coffee", 2200 },
        };

        public static int Total(IEnumerable<string> items)
        {
            int total = 0;
            foreach (var item in items) total += Prices[item];
            return total;
        }

        public static (CheckoutEffect effect, int rule) Decide(string visit, CheckoutChoice choice) => (visit, choice) switch
        {
            ("raincoatBad", CheckoutChoice.Charge) => (CheckoutEffect.Die, 3),
            ("raincoatChild", CheckoutChoice.SendHome or CheckoutChoice.RegisterBroken) => (CheckoutEffect.Strike, 3),
            ("loneChild", CheckoutChoice.Charge) => (CheckoutEffect.Strike, 4),
            ("childParent", CheckoutChoice.SendHome) => (CheckoutEffect.Complain, 0),
            ("noShadow", CheckoutChoice.Charge) => (CheckoutEffect.Die, 15),
            ("childMom", CheckoutChoice.Charge) => (CheckoutEffect.Die, 18),
            ("restroom", CheckoutChoice.AllowStorage) => (CheckoutEffect.Strike, 9),
            (_, CheckoutChoice.Charge) => (CheckoutEffect.Sale, 0),
            _ => (CheckoutEffect.Refusal, 0),
        };
    }
}
