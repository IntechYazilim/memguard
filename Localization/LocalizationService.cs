using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemGuard.Localization
{
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tr"] = BuildTurkishTranslations(),
            ["en"] = BuildEnglishTranslations(),
            ["es"] = BuildSpanishTranslations(),
            ["fr"] = BuildFrenchTranslations()
        };

        private string _currentLanguage = "en";

        public static LocalizationService Instance { get; } = new();

        public string CurrentLanguage => _currentLanguage;

        public string this[string key] => Translate(key);

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetLanguage(string language)
        {
            var normalized = NormalizeLanguage(language);
            if (_currentLanguage == normalized)
            {
                return;
            }

            _currentLanguage = normalized;
            OnPropertyChanged(nameof(CurrentLanguage));
            OnPropertyChanged("Item[]");
        }

        public string Translate(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var current) && current.TryGetValue(key, out var value))
            {
                return value;
            }

            if (_translations["en"].TryGetValue(key, out var fallback))
            {
                return fallback;
            }

            return key;
        }

        private static string NormalizeLanguage(string? language)
        {
            return (language ?? string.Empty).ToLowerInvariant() switch
            {
                "tr" => "tr",
                "es" => "es",
                "fr" => "fr",
                _ => "en"
            };
        }

        private static Dictionary<string, string> BuildTurkishTranslations()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Nav.Dashboard"] = "Pano",
                ["Nav.Memory"] = "Bellek",
                ["Nav.Processes"] = "Süreçler",
                ["Nav.Cleaner"] = "Temizleyici",
                ["Nav.Startup"] = "Başlangıç",
                ["Nav.GameMode"] = "Oyun Modu",
                ["Nav.Settings"] = "Ayarlar",
                ["Sidebar.SystemStatus"] = "Sistem Durumu:",
                ["Sidebar.Version"] = "Sürüm:",

                ["Settings.Kicker"] = "AYARLAR",
                ["Settings.Title"] = "Kontrol Paneli",
                ["Settings.Subtitle"] = "Tema, dil, arka plan davranışı ve otomatik RAM optimizasyonunu buradan yönet.",
                ["Settings.General"] = "Genel",
                ["Settings.GeneralDesc"] = "Uygulamanın genel davranış ayarları.",
                ["Settings.StartWithWindows"] = "Windows ile Başlat",
                ["Settings.StartWithWindowsDesc"] = "Windows açıldığında MemGuard otomatik başlasın.",
                ["Settings.MinimizeToTray"] = "Tepsiye Küçült",
                ["Settings.MinimizeToTrayDesc"] = "Kapatınca tamamen çıkmak yerine arkada çalışsın.",
                ["Settings.Notifications"] = "Bildirimler",
                ["Settings.NotificationsDesc"] = "Optimizasyon ve işlem sonuç bildirimlerini göster.",
                ["Settings.AutoMonitoring"] = "Otomatik İzleme",
                ["Settings.AutoMonitoringDesc"] = "Arka planda sistem durumunu sürekli takip et.",
                ["Settings.HardwareAcceleration"] = "Donanım Hızlandırma",
                ["Settings.HardwareAccelerationDesc"] = "Animasyonlar ve grafikler için GPU hızlandırma kullan.",
                ["Settings.Themes"] = "Temalar",
                ["Settings.ThemesDesc"] = "Görünümü anında değiştir.",
                ["Settings.ActiveTheme"] = "Aktif Tema",
                ["Settings.Language"] = "Dil",
                ["Settings.LanguageDesc"] = "Arayüz dilini seç.",
                ["Settings.SmartAutoOptimize"] = "Akıllı Otomatik Optimizasyon",
                ["Settings.SmartAutoOptimizeDesc"] = "RAM belli seviyeyi geçince otomatik optimize etsin.",
                ["Settings.EnableAutoOptimize"] = "Otomatik Optimize Et",
                ["Settings.EnableAutoOptimizeDesc"] = "RAM kullanımı eşiği aşılınca otomatik optimizasyon başlat.",
                ["Settings.TriggerThreshold"] = "Tetikleme Eşiği",
                ["Settings.TriggerThresholdDesc"] = "RAM kullanımı bu yüzdeyi geçince devreye girer.",
                ["Settings.BaseInterval"] = "Temel Aralık",
                ["Settings.BaseIntervalDesc"] = "Sabit modda kaç dakikada bir optimize edileceği.",
                ["Settings.AdaptiveInterval"] = "Uyarlanabilir Aralık",
                ["Settings.AdaptiveIntervalDesc"] = "RAM daha çok dolarsa bekleme süresini otomatik kısalt.",

                ["Memory.Kicker"] = "BELLEK YONETIMI",
                ["Memory.Title"] = "Güvenli RAM Optimizasyonu",
                ["Memory.TotalRam"] = "Toplam Fiziksel RAM",
                ["Memory.UsedMemory"] = "Kullanılan Bellek",
                ["Memory.AvailableMemory"] = "Boş Bellek",
                ["Memory.Optimize"] = "BELLEĞİ OPTİMİZE ET",
                ["Memory.Log"] = "OPTİMİZASYON GÜNLÜĞÜ",
                ["Memory.Success"] = "OPTİMİZASYON TAMAMLANDI",
                ["Memory.SystemHealth"] = "SİSTEM SAĞLIĞI",

                ["Dashboard.Kicker"] = "SİSTEM DURUMU",
                ["Dashboard.CpuUsage"] = "CPU KULLANIMI",
                ["Dashboard.RamUsage"] = "RAM KULLANIMI",
                ["Dashboard.DiskUsage"] = "DİSK KULLANIMI",
                ["Dashboard.SystemUptime"] = "ÇALIŞMA SÜRESİ",
                ["Dashboard.RamPerformance"] = "RAM PERFORMANSI (60 SN)",
                ["Dashboard.CpuPerformance"] = "CPU PERFORMANSI (60SN)",

                ["Startup.Kicker"] = "BAŞLANGIÇ YÖNETİCİSİ",
                ["Startup.Title"] = "Windows Otomatik Başlangıç Optimizasyonu",
                ["Startup.Status"] = "Durum",
                ["Startup.Application"] = "Uygulama",
                ["Startup.Publisher"] = "Yayıncı",
                ["Startup.Location"] = "Konum",
                ["Startup.Impact"] = "Başlangıç Etkisi",
                ["Startup.Loading"] = "Başlangıç kayıtları yükleniyor...",
                ["Startup.InfoTitle"] = "Başlangıç Kontrol Merkezi",
                ["Startup.InfoDesc"] = "Sistem seviyesindeki girişler yönetici izni ister. Kullanıcı ve sistem başlangıç klasörleri artık orijinal konumlarına geri yüklenir.",
                ["Startup.Tracked"] = "izleniyor",

                ["Processes.Kicker"] = "İŞLEM YÖNETİCİSİ",
                ["Processes.Title"] = "Aktif Uygulamalar",
                ["Processes.SortMemory"] = "Bellek",
                ["Processes.SortCpu"] = "CPU",
                ["Processes.HighMemory"] = "YÜKSEK BELLEK TÜKETİMİ ALGILANDI",
                ["Processes.CloseApp"] = "Kapat",
                ["Processes.Application"] = "Uygulama",
                ["Processes.RamUsage"] = "RAM Kullanimi",
                ["Processes.CpuUsage"] = "CPU",
                ["Processes.Publisher"] = "Yayıncı",
                ["Processes.Status"] = "Durum",
                ["Processes.Loading"] = "Süreçler taranıyor...",
                ["Processes.ProcessDetails"] = "İŞLEM DETAYLARI",
                ["Processes.ProcessName"] = "İşlem Adı",
                ["Processes.StartTime"] = "Başlangıç Zamanı",
                ["Processes.ExecutablePath"] = "Çalıştırılabilir Yol",
                ["Processes.CloseApplication"] = "UYGULAMAYI KAPAT",
                ["Processes.StatusSystemProtected"] = "Sistem Korumalı",
                ["Processes.StatusUserApplication"] = "Kullanıcı Uygulaması",

                ["Theme.Dark"] = "Koyu",
                ["Theme.Ocean"] = "Okyanus",
                ["Theme.Sunset"] = "Gün Batımı",
                ["Theme.Aurora"] = "Aurora",
                ["Theme.Graphite"] = "Graphite",
                ["Theme.Volt"] = "Volt",
                ["Language.Turkish"] = "Türkçe",
                ["Language.English"] = "English",
                ["Language.Spanish"] = "Español",
                ["Language.French"] = "Français",

                ["Common.Used"] = "kullanıldı",
                ["Status.Analyzing"] = "Analiz ediliyor...",
                ["Status.Excellent"] = "Mükemmel",
                ["Status.Good"] = "İyi",
                ["Status.Recommended"] = "Optimizasyon Önerilir",
                ["Status.HighMemory"] = "Yüksek Bellek Kullanımı",

                ["Toast.ThemeUpdated"] = "Tema Güncellendi",
                ["Toast.ThemeApplied"] = "{0} teması etkin.",
                ["Toast.LanguageUpdated"] = "Dil Güncellendi",
                ["Toast.LanguageApplied"] = "{0} etkin.",
                ["Toast.MemoryOptimized"] = "Bellek Optimize Edildi",
                ["Toast.MemoryChecked"] = "Bellek Kontrol Edildi"
            };
        }

        private static Dictionary<string, string> BuildEnglishTranslations()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Nav.Dashboard"] = "Dashboard",
                ["Nav.Memory"] = "Memory",
                ["Nav.Processes"] = "Processes",
                ["Nav.Cleaner"] = "Cleaner",
                ["Nav.Startup"] = "Startup",
                ["Nav.GameMode"] = "Game Mode",
                ["Nav.Settings"] = "Settings",
                ["Sidebar.SystemStatus"] = "System Status:",
                ["Sidebar.Version"] = "Version:",

                ["Settings.Kicker"] = "SETTINGS",
                ["Settings.Title"] = "Control Panel",
                ["Settings.Subtitle"] = "Manage theme, language, background behavior, and automatic RAM optimization here.",
                ["Settings.General"] = "General",
                ["Settings.GeneralDesc"] = "General behavior settings for the app.",
                ["Settings.StartWithWindows"] = "Start with Windows",
                ["Settings.StartWithWindowsDesc"] = "Launch MemGuard automatically when Windows starts.",
                ["Settings.MinimizeToTray"] = "Minimize to Tray",
                ["Settings.MinimizeToTrayDesc"] = "Keep the app running in the tray when you close it.",
                ["Settings.Notifications"] = "Notifications",
                ["Settings.NotificationsDesc"] = "Show optimization and action result notifications.",
                ["Settings.AutoMonitoring"] = "Automatic Monitoring",
                ["Settings.AutoMonitoringDesc"] = "Continuously monitor system state in the background.",
                ["Settings.HardwareAcceleration"] = "Hardware Acceleration",
                ["Settings.HardwareAccelerationDesc"] = "Use GPU acceleration for animations and charts.",
                ["Settings.Themes"] = "Themes",
                ["Settings.ThemesDesc"] = "Change the visual style instantly.",
                ["Settings.ActiveTheme"] = "Active Theme",
                ["Settings.Language"] = "Language",
                ["Settings.LanguageDesc"] = "Choose the UI language.",
                ["Settings.SmartAutoOptimize"] = "Smart Auto Optimize",
                ["Settings.SmartAutoOptimizeDesc"] = "Automatically optimize when RAM crosses a threshold.",
                ["Settings.EnableAutoOptimize"] = "Enable Auto Optimize",
                ["Settings.EnableAutoOptimizeDesc"] = "Start automatic optimization when RAM usage crosses the threshold.",
                ["Settings.TriggerThreshold"] = "Trigger Threshold",
                ["Settings.TriggerThresholdDesc"] = "Runs when RAM usage exceeds this percentage.",
                ["Settings.BaseInterval"] = "Base Interval",
                ["Settings.BaseIntervalDesc"] = "How often to optimize in fixed mode.",
                ["Settings.AdaptiveInterval"] = "Adaptive Interval",
                ["Settings.AdaptiveIntervalDesc"] = "Shorten wait time automatically when RAM fills up.",

                ["Memory.Kicker"] = "MEMORY MANAGEMENT",
                ["Memory.Title"] = "Safe RAM Optimization",
                ["Memory.TotalRam"] = "Total Physical RAM",
                ["Memory.UsedMemory"] = "Used Memory",
                ["Memory.AvailableMemory"] = "Available Memory",
                ["Memory.Optimize"] = "OPTIMIZE MEMORY",
                ["Memory.Log"] = "OPTIMIZATION LOG",
                ["Memory.Success"] = "OPTIMIZATION SUCCESSFUL",
                ["Memory.SystemHealth"] = "SYSTEM HEALTH",

                ["Dashboard.Kicker"] = "SYSTEM STATUS",
                ["Dashboard.CpuUsage"] = "CPU USAGE",
                ["Dashboard.RamUsage"] = "RAM USAGE",
                ["Dashboard.DiskUsage"] = "DISK USAGE",
                ["Dashboard.SystemUptime"] = "SYSTEM UPTIME",
                ["Dashboard.RamPerformance"] = "RAM PERFORMANCE (60S)",
                ["Dashboard.CpuPerformance"] = "CPU PERFORMANCE (60S)",

                ["Startup.Kicker"] = "STARTUP MANAGER",
                ["Startup.Title"] = "Windows Autostart Optimization",
                ["Startup.Status"] = "Status",
                ["Startup.Application"] = "Application",
                ["Startup.Publisher"] = "Publisher",
                ["Startup.Location"] = "Location",
                ["Startup.Impact"] = "Startup Impact",
                ["Startup.Loading"] = "Loading startup configurations...",
                ["Startup.InfoTitle"] = "Startup Control Center",
                ["Startup.InfoDesc"] = "System-level entries need administrator rights. User and system startup folders now restore to their original location.",
                ["Startup.Tracked"] = "tracked",

                ["Processes.Kicker"] = "PROCESS MANAGER",
                ["Processes.Title"] = "Active Applications",
                ["Processes.SortMemory"] = "Memory",
                ["Processes.SortCpu"] = "CPU",
                ["Processes.HighMemory"] = "HIGH MEMORY CONSUMPTION DETECTED",
                ["Processes.CloseApp"] = "Close App",
                ["Processes.Application"] = "Application",
                ["Processes.RamUsage"] = "RAM Usage",
                ["Processes.CpuUsage"] = "CPU",
                ["Processes.Publisher"] = "Publisher",
                ["Processes.Status"] = "Status",
                ["Processes.Loading"] = "Scanning processes...",
                ["Processes.ProcessDetails"] = "PROCESS DETAILS",
                ["Processes.ProcessName"] = "Process Name",
                ["Processes.StartTime"] = "Start Time",
                ["Processes.ExecutablePath"] = "Executable Path",
                ["Processes.CloseApplication"] = "CLOSE APPLICATION",
                ["Processes.StatusSystemProtected"] = "System Protected",
                ["Processes.StatusUserApplication"] = "User Application",

                ["Theme.Dark"] = "Dark",
                ["Theme.Ocean"] = "Ocean",
                ["Theme.Sunset"] = "Sunset",
                ["Theme.Aurora"] = "Aurora",
                ["Theme.Graphite"] = "Graphite",
                ["Theme.Volt"] = "Volt",
                ["Language.Turkish"] = "Turkish",
                ["Language.English"] = "English",
                ["Language.Spanish"] = "Spanish",
                ["Language.French"] = "French",

                ["Common.Used"] = "Used",
                ["Status.Analyzing"] = "Analyzing...",
                ["Status.Excellent"] = "Excellent",
                ["Status.Good"] = "Good",
                ["Status.Recommended"] = "Optimization Recommended",
                ["Status.HighMemory"] = "High Memory Usage",

                ["Toast.ThemeUpdated"] = "Theme Updated",
                ["Toast.ThemeApplied"] = "{0} theme applied.",
                ["Toast.LanguageUpdated"] = "Language Updated",
                ["Toast.LanguageApplied"] = "{0} enabled.",
                ["Toast.MemoryOptimized"] = "Memory Optimized",
                ["Toast.MemoryChecked"] = "Memory Checked"
            };
        }

        private static Dictionary<string, string> BuildSpanishTranslations()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Nav.Dashboard"] = "Panel",
                ["Nav.Memory"] = "Memoria",
                ["Nav.Processes"] = "Procesos",
                ["Nav.Cleaner"] = "Limpieza",
                ["Nav.Startup"] = "Inicio",
                ["Nav.GameMode"] = "Modo Juego",
                ["Nav.Settings"] = "Ajustes",
                ["Sidebar.SystemStatus"] = "Estado del sistema:",
                ["Sidebar.Version"] = "Version:",
                ["Settings.Kicker"] = "AJUSTES",
                ["Settings.Title"] = "Panel de Control",
                ["Settings.Subtitle"] = "Administra el tema, idioma, comportamiento en segundo plano y la optimizacion automatica de RAM aqui.",
                ["Settings.General"] = "General",
                ["Settings.GeneralDesc"] = "Configuracion general de la aplicacion.",
                ["Settings.StartWithWindows"] = "Iniciar con Windows",
                ["Settings.StartWithWindowsDesc"] = "Abrir MemGuard automaticamente al iniciar Windows.",
                ["Settings.MinimizeToTray"] = "Minimizar a la bandeja",
                ["Settings.MinimizeToTrayDesc"] = "Mantener la app en segundo plano al cerrarla.",
                ["Settings.Notifications"] = "Notificaciones",
                ["Settings.NotificationsDesc"] = "Mostrar avisos sobre optimizacion y acciones.",
                ["Settings.AutoMonitoring"] = "Monitoreo automatico",
                ["Settings.AutoMonitoringDesc"] = "Supervisar el sistema continuamente en segundo plano.",
                ["Settings.HardwareAcceleration"] = "Aceleracion por hardware",
                ["Settings.HardwareAccelerationDesc"] = "Usar aceleracion GPU para animaciones y graficos.",
                ["Settings.Themes"] = "Temas",
                ["Settings.ThemesDesc"] = "Cambia el estilo visual al instante.",
                ["Settings.ActiveTheme"] = "Tema activo",
                ["Settings.Language"] = "Idioma",
                ["Settings.LanguageDesc"] = "Elige el idioma de la interfaz.",
                ["Settings.SmartAutoOptimize"] = "Optimizacion automatica inteligente",
                ["Settings.SmartAutoOptimizeDesc"] = "Optimiza automaticamente cuando la RAM supera un umbral.",
                ["Settings.EnableAutoOptimize"] = "Activar auto optimizacion",
                ["Settings.EnableAutoOptimizeDesc"] = "Inicia la optimizacion automatica al superar el umbral.",
                ["Settings.TriggerThreshold"] = "Umbral de activacion",
                ["Settings.TriggerThresholdDesc"] = "Se ejecuta cuando el uso de RAM supera este porcentaje.",
                ["Settings.BaseInterval"] = "Intervalo base",
                ["Settings.BaseIntervalDesc"] = "Cada cuanto optimizar en modo fijo.",
                ["Settings.AdaptiveInterval"] = "Intervalo adaptativo",
                ["Settings.AdaptiveIntervalDesc"] = "Reducir la espera automaticamente cuando la RAM se llena.",
                ["Memory.Kicker"] = "GESTION DE MEMORIA",
                ["Memory.Title"] = "Optimizacion segura de RAM",
                ["Memory.TotalRam"] = "RAM fisica total",
                ["Memory.UsedMemory"] = "Memoria usada",
                ["Memory.AvailableMemory"] = "Memoria disponible",
                ["Memory.Optimize"] = "OPTIMIZAR MEMORIA",
                ["Memory.Log"] = "REGISTRO DE OPTIMIZACION",
                ["Memory.Success"] = "OPTIMIZACION COMPLETADA",
                ["Memory.SystemHealth"] = "SALUD DEL SISTEMA",
                ["Dashboard.Kicker"] = "ESTADO DEL SISTEMA",
                ["Dashboard.CpuUsage"] = "USO DE CPU",
                ["Dashboard.RamUsage"] = "USO DE RAM",
                ["Dashboard.DiskUsage"] = "USO DE DISCO",
                ["Dashboard.SystemUptime"] = "TIEMPO ACTIVO",
                ["Dashboard.RamPerformance"] = "RENDIMIENTO RAM (60S)",
                ["Dashboard.CpuPerformance"] = "RENDIMIENTO CPU (60S)",
                ["Startup.Kicker"] = "GESTOR DE INICIO",
                ["Startup.Title"] = "Optimizacion de inicio de Windows",
                ["Startup.Status"] = "Estado",
                ["Startup.Application"] = "Aplicacion",
                ["Startup.Publisher"] = "Editor",
                ["Startup.Location"] = "Ubicacion",
                ["Startup.Impact"] = "Impacto",
                ["Startup.Loading"] = "Cargando elementos de inicio...",
                ["Startup.InfoTitle"] = "Centro de control de inicio",
                ["Startup.InfoDesc"] = "Las entradas del sistema requieren permisos de administrador. Las carpetas de inicio del usuario y del sistema ahora vuelven a su ubicacion original.",
                ["Startup.Tracked"] = "en seguimiento",
                ["Processes.Kicker"] = "GESTOR DE PROCESOS",
                ["Processes.Title"] = "Aplicaciones activas",
                ["Processes.SortMemory"] = "Memoria",
                ["Processes.SortCpu"] = "CPU",
                ["Processes.HighMemory"] = "SE DETECTO ALTO CONSUMO DE MEMORIA",
                ["Processes.CloseApp"] = "Cerrar",
                ["Processes.Application"] = "Aplicacion",
                ["Processes.RamUsage"] = "Uso de RAM",
                ["Processes.CpuUsage"] = "CPU",
                ["Processes.Publisher"] = "Editor",
                ["Processes.Status"] = "Estado",
                ["Processes.Loading"] = "Analizando procesos...",
                ["Processes.ProcessDetails"] = "DETALLES DEL PROCESO",
                ["Processes.ProcessName"] = "Nombre del proceso",
                ["Processes.StartTime"] = "Hora de inicio",
                ["Processes.ExecutablePath"] = "Ruta ejecutable",
                ["Processes.CloseApplication"] = "CERRAR APLICACION",
                ["Processes.StatusSystemProtected"] = "Sistema protegido",
                ["Processes.StatusUserApplication"] = "Aplicacion del usuario",
                ["Theme.Dark"] = "Dark",
                ["Theme.Ocean"] = "Ocean",
                ["Theme.Sunset"] = "Sunset",
                ["Theme.Aurora"] = "Aurora",
                ["Theme.Graphite"] = "Graphite",
                ["Theme.Volt"] = "Volt",
                ["Language.Turkish"] = "Turco",
                ["Language.English"] = "Ingles",
                ["Language.Spanish"] = "Espanol",
                ["Language.French"] = "Frances",
                ["Common.Used"] = "usado",
                ["Status.Analyzing"] = "Analizando...",
                ["Status.Excellent"] = "Excelente",
                ["Status.Good"] = "Bueno",
                ["Status.Recommended"] = "Optimizacion recomendada",
                ["Status.HighMemory"] = "Uso alto de memoria",
                ["Toast.ThemeUpdated"] = "Tema actualizado",
                ["Toast.ThemeApplied"] = "Tema {0} aplicado.",
                ["Toast.LanguageUpdated"] = "Idioma actualizado",
                ["Toast.LanguageApplied"] = "{0} activado.",
                ["Toast.MemoryOptimized"] = "Memoria optimizada",
                ["Toast.MemoryChecked"] = "Memoria verificada"
            };
        }

        private static Dictionary<string, string> BuildFrenchTranslations()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Nav.Dashboard"] = "Tableau",
                ["Nav.Memory"] = "Memoire",
                ["Nav.Processes"] = "Processus",
                ["Nav.Cleaner"] = "Nettoyage",
                ["Nav.Startup"] = "Demarrage",
                ["Nav.GameMode"] = "Mode Jeu",
                ["Nav.Settings"] = "Parametres",
                ["Sidebar.SystemStatus"] = "Etat du systeme:",
                ["Sidebar.Version"] = "Version:",
                ["Settings.Kicker"] = "PARAMETRES",
                ["Settings.Title"] = "Panneau de Controle",
                ["Settings.Subtitle"] = "Gerez ici le theme, la langue, le comportement en arriere-plan et l'optimisation automatique de la RAM.",
                ["Settings.General"] = "General",
                ["Settings.GeneralDesc"] = "Parametres generaux de l'application.",
                ["Settings.StartWithWindows"] = "Demarrer avec Windows",
                ["Settings.StartWithWindowsDesc"] = "Lancer MemGuard automatiquement au demarrage de Windows.",
                ["Settings.MinimizeToTray"] = "Reduire dans la zone",
                ["Settings.MinimizeToTrayDesc"] = "Laisser l'application en arriere-plan a la fermeture.",
                ["Settings.Notifications"] = "Notifications",
                ["Settings.NotificationsDesc"] = "Afficher les notifications d'optimisation et d'action.",
                ["Settings.AutoMonitoring"] = "Surveillance automatique",
                ["Settings.AutoMonitoringDesc"] = "Surveiller le systeme en continu en arriere-plan.",
                ["Settings.HardwareAcceleration"] = "Acceleration materielle",
                ["Settings.HardwareAccelerationDesc"] = "Utiliser le GPU pour les animations et graphiques.",
                ["Settings.Themes"] = "Themes",
                ["Settings.ThemesDesc"] = "Changez instantanement le style visuel.",
                ["Settings.ActiveTheme"] = "Theme actif",
                ["Settings.Language"] = "Langue",
                ["Settings.LanguageDesc"] = "Choisissez la langue de l'interface.",
                ["Settings.SmartAutoOptimize"] = "Auto-optimisation intelligente",
                ["Settings.SmartAutoOptimizeDesc"] = "Optimise automatiquement lorsque la RAM depasse un seuil.",
                ["Settings.EnableAutoOptimize"] = "Activer l'auto-optimisation",
                ["Settings.EnableAutoOptimizeDesc"] = "Lancer l'optimisation auto lorsque le seuil est depasse.",
                ["Settings.TriggerThreshold"] = "Seuil de declenchement",
                ["Settings.TriggerThresholdDesc"] = "Se lance lorsque l'utilisation RAM depasse ce pourcentage.",
                ["Settings.BaseInterval"] = "Intervalle de base",
                ["Settings.BaseIntervalDesc"] = "Frequence d'optimisation en mode fixe.",
                ["Settings.AdaptiveInterval"] = "Intervalle adaptatif",
                ["Settings.AdaptiveIntervalDesc"] = "Reduire automatiquement l'attente si la RAM se remplit.",
                ["Memory.Kicker"] = "GESTION DE LA MEMOIRE",
                ["Memory.Title"] = "Optimisation RAM securisee",
                ["Memory.TotalRam"] = "RAM physique totale",
                ["Memory.UsedMemory"] = "Memoire utilisee",
                ["Memory.AvailableMemory"] = "Memoire disponible",
                ["Memory.Optimize"] = "OPTIMISER LA MEMOIRE",
                ["Memory.Log"] = "JOURNAL D'OPTIMISATION",
                ["Memory.Success"] = "OPTIMISATION TERMINEE",
                ["Memory.SystemHealth"] = "SANTE DU SYSTEME",
                ["Dashboard.Kicker"] = "ETAT DU SYSTEME",
                ["Dashboard.CpuUsage"] = "UTILISATION CPU",
                ["Dashboard.RamUsage"] = "UTILISATION RAM",
                ["Dashboard.DiskUsage"] = "UTILISATION DISQUE",
                ["Dashboard.SystemUptime"] = "TEMPS D'ACTIVITE",
                ["Dashboard.RamPerformance"] = "PERFORMANCE RAM (60S)",
                ["Dashboard.CpuPerformance"] = "PERFORMANCE CPU (60S)",
                ["Startup.Kicker"] = "GESTIONNAIRE DE DEMARRAGE",
                ["Startup.Title"] = "Optimisation du demarrage Windows",
                ["Startup.Status"] = "Etat",
                ["Startup.Application"] = "Application",
                ["Startup.Publisher"] = "Editeur",
                ["Startup.Location"] = "Emplacement",
                ["Startup.Impact"] = "Impact",
                ["Startup.Loading"] = "Chargement des elements de demarrage...",
                ["Startup.InfoTitle"] = "Centre de controle du demarrage",
                ["Startup.InfoDesc"] = "Les elements systeme necessitent les droits administrateur. Les dossiers de demarrage utilisateur et systeme reviennent maintenant a leur emplacement d'origine.",
                ["Startup.Tracked"] = "suivis",
                ["Processes.Kicker"] = "GESTIONNAIRE DE PROCESSUS",
                ["Processes.Title"] = "Applications actives",
                ["Processes.SortMemory"] = "Memoire",
                ["Processes.SortCpu"] = "CPU",
                ["Processes.HighMemory"] = "FORTE CONSOMMATION MEMOIRE DETECTEE",
                ["Processes.CloseApp"] = "Fermer",
                ["Processes.Application"] = "Application",
                ["Processes.RamUsage"] = "Utilisation RAM",
                ["Processes.CpuUsage"] = "CPU",
                ["Processes.Publisher"] = "Editeur",
                ["Processes.Status"] = "Etat",
                ["Processes.Loading"] = "Analyse des processus...",
                ["Processes.ProcessDetails"] = "DETAILS DU PROCESSUS",
                ["Processes.ProcessName"] = "Nom du processus",
                ["Processes.StartTime"] = "Heure de demarrage",
                ["Processes.ExecutablePath"] = "Chemin executable",
                ["Processes.CloseApplication"] = "FERMER L'APPLICATION",
                ["Processes.StatusSystemProtected"] = "Systeme protege",
                ["Processes.StatusUserApplication"] = "Application utilisateur",
                ["Theme.Dark"] = "Dark",
                ["Theme.Ocean"] = "Ocean",
                ["Theme.Sunset"] = "Sunset",
                ["Theme.Aurora"] = "Aurora",
                ["Theme.Graphite"] = "Graphite",
                ["Theme.Volt"] = "Volt",
                ["Language.Turkish"] = "Turc",
                ["Language.English"] = "Anglais",
                ["Language.Spanish"] = "Espagnol",
                ["Language.French"] = "Francais",
                ["Common.Used"] = "utilisee",
                ["Status.Analyzing"] = "Analyse en cours...",
                ["Status.Excellent"] = "Excellent",
                ["Status.Good"] = "Bon",
                ["Status.Recommended"] = "Optimisation recommandee",
                ["Status.HighMemory"] = "Utilisation memoire elevee",
                ["Toast.ThemeUpdated"] = "Theme mis a jour",
                ["Toast.ThemeApplied"] = "Theme {0} applique.",
                ["Toast.LanguageUpdated"] = "Langue mise a jour",
                ["Toast.LanguageApplied"] = "{0} active.",
                ["Toast.MemoryOptimized"] = "Memoire optimisee",
                ["Toast.MemoryChecked"] = "Memoire verifiee"
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
