class Config {
  // Configurable base URL.
  // Change this depending on the environment (emulator vs physical device).
  // For physical Android device testing on local Wi-Fi, use your PC's IP, e.g., 192.168.x.x
  // For Android Emulator, use 10.0.2.2
  // Make sure to include the port (e.g., 5160 for HTTP)
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5160/api',
  );
}
