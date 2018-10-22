using System;
namespace ModTheGungeon {
    public static class EventHooks {
        private static Logger _Logger = new Logger("EventHooks");

        public static Action<MainMenuFoyerController> MainMenuLoadedFirstTime;
        public static void InvokeMainMenuLoadedFirstTime(MainMenuFoyerController menu) {
            _Logger.Debug(nameof(MainMenuLoadedFirstTime));
            MainMenuLoadedFirstTime?.Invoke(menu);
        }

        public static Action GameStarted;
        public static void InvokeGameStarted() {
            _Logger.Debug(nameof(GameStarted));
            GameStarted?.Invoke();
        }
    }
}
