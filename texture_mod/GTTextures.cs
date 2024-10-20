using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace GorillaTextureReplacer
{
    [BepInPlugin("com.yourname.gorillatag.texturereplacer", "Gorilla Tag Texture Replacer", "1.0.0")]
    [BepInProcess("Gorilla Tag.exe")]
    public class TextureReplacementMod : BaseUnityPlugin
    {
        private string texturePath = "";
        private bool showGUI = true;
        private Rect windowRect = new Rect(20f, 20f, 500f, 900f);
        private string statusMessage = "";
        private List<Camera> allCameras = new List<Camera>();
        private int currentCameraIndex;
        private string lookedAtObjectInfo = "";
        private Vector2 scrollPosition;
        private Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
        private float tileSize = 1f;
        private Texture2D playerTexture;
        private bool changePlayerTexture;
        private string lookedAtTextureName = "";
        private Vector2 textureNameScrollPosition;
        private float blurAmount;
        private float textureScale = 1f;
        private float pixelationAmount = 1f;
        private string texturePackPath = "";
        private float qualitySlider = 1f;
        private int repetitionCount = 1;
        private float darknessAmount = 0.5f;
        private float contrastAmount = 1.2f;
        private bool enableRandomTextures = false;
        private float randomTextureInterval = 5f;
        private float lastRandomTextureTime = 0f;
        private List<string> texturePool = new List<string>();
        private bool enableTextureAnimation = false;
        private float textureAnimationSpeed = 1f;
        private int currentAnimationFrame = 0;
        private List<Texture2D> animationFrames = new List<Texture2D>();
        private bool enableProceduralTextures = false;
        private float proceduralNoiseScale = 1f;
        private Color proceduralColor1 = Color.white;
        private Color proceduralColor2 = Color.black;

        private string texturesFolder;
        private List<string> texturePacks = new List<string>();
        private int selectedTexturePackIndex = -1;
        private Vector2 texturePackScrollPosition;

        private bool showHowToUse = false;
        private string howToUseText = "";

        private void Awake()
        {
            Debug.Log("Texture Replacer mod is awake!");
            texturesFolder = Path.Combine(Paths.PluginPath, "Textures");
            EnsureTexturesFolderExists();
            LoadTexturePacks();
            InitializeHowToUseText();
        }

        private void EnsureTexturesFolderExists()
        {
            if (!Directory.Exists(texturesFolder))
            {
                Directory.CreateDirectory(texturesFolder);
                Debug.Log("Created Textures folder: " + texturesFolder);
            }
        }

        private void LoadTexturePacks()
        {
            texturePacks.Clear();
            if (Directory.Exists(texturesFolder))
            {
                texturePacks.AddRange(Directory.GetFiles(texturesFolder, "*.zip"));
            }
        }

        private void InitializeHowToUseText()
        {
            howToUseText = "How to Use Texture Replacer:\n\n" +
                "1. Enter the path to a texture file in the 'Texture Path' field.\n" +
                "2. Click 'Load Texture' to load the texture.\n" +
                "3. Adjust texture modifications using the sliders.\n" +
                "4. Click 'Apply Texture Modifications' to apply changes.\n" +
                "5. Use 'Change Player Texture' to modify your character's texture.\n" +
                "6. Select a camera from the list to change the active camera.\n" +
                "7. The 'Looked At Object' and 'Looked At Texture' fields show information about the object you're looking at.\n" +
                "8. To use texture packs, place .zip files in the 'Textures' folder in your plugins directory.\n" +
                "9. Select a texture pack from the list and click 'Apply Selected Texture Pack'.\n" +
                "10. Press F1 to show/hide the main interface.\n\n" +
                "Experiment with different settings to create unique visual effects!";
        }

        private void Start()
        {
            RefreshCameraList();
        }

        private void RefreshCameraList()
        {
            allCameras = FindObjectsOfType<Camera>().ToList();
            currentCameraIndex = 0;
        }

        private void OnGUI()
        {
            if (showGUI)
            {
                windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Texture Replacer");
            }
        }
        private void DrawWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Texture Path:");
            texturePath = GUILayout.TextField(texturePath);
            if (GUILayout.Button("Load Texture"))
            {
                LoadTexture();
            }

            GUILayout.Space(10);

            GUILayout.Label("Texture Packs:");
            texturePackScrollPosition = GUILayout.BeginScrollView(texturePackScrollPosition, GUILayout.Height(100));
            for (int i = 0; i < texturePacks.Count; i++)
            {
                if (GUILayout.Toggle(selectedTexturePackIndex == i, Path.GetFileNameWithoutExtension(texturePacks[i])))
                {
                    selectedTexturePackIndex = i;
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Apply Selected Texture Pack"))
            {
                if (selectedTexturePackIndex >= 0 && selectedTexturePackIndex < texturePacks.Count)
                {
                    ImportTexturePack(texturePacks[selectedTexturePackIndex]);
                }
                else
                {
                    statusMessage = "No texture pack selected.";
                }
            }

            GUILayout.Space(10);

            GUILayout.Label("Texture Modifications:");
            tileSize = GUILayout.HorizontalSlider(tileSize, 0.1f, 10f);
            GUILayout.Label("Tile Size: " + tileSize.ToString("F2"));
            blurAmount = GUILayout.HorizontalSlider(blurAmount, 0f, 5f);
            GUILayout.Label("Blur Amount: " + blurAmount.ToString("F2"));
            textureScale = GUILayout.HorizontalSlider(textureScale, 0.1f, 2f);
            GUILayout.Label("Texture Scale: " + textureScale.ToString("F2"));
            pixelationAmount = GUILayout.HorizontalSlider(pixelationAmount, 1f, 64f);
            GUILayout.Label("Pixelation Amount: " + pixelationAmount.ToString("F2"));
            qualitySlider = GUILayout.HorizontalSlider(qualitySlider, 0.1f, 1f);
            GUILayout.Label("Quality: " + qualitySlider.ToString("F2"));
            repetitionCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(repetitionCount, 1, 10));
            GUILayout.Label("Repetition Count: " + repetitionCount);
            darknessAmount = GUILayout.HorizontalSlider(darknessAmount, 0f, 1f);
            GUILayout.Label("Darkness: " + darknessAmount.ToString("F2"));
            contrastAmount = GUILayout.HorizontalSlider(contrastAmount, 0.5f, 2f);
            GUILayout.Label("Contrast: " + contrastAmount.ToString("F2"));

            GUILayout.Space(10);

            enableRandomTextures = GUILayout.Toggle(enableRandomTextures, "Enable Random Textures");
            if (enableRandomTextures)
            {
                randomTextureInterval = GUILayout.HorizontalSlider(randomTextureInterval, 1f, 60f);
                GUILayout.Label("Random Texture Interval: " + randomTextureInterval.ToString("F2") + " seconds");
            }

            enableTextureAnimation = GUILayout.Toggle(enableTextureAnimation, "Enable Texture Animation");
            if (enableTextureAnimation)
            {
                textureAnimationSpeed = GUILayout.HorizontalSlider(textureAnimationSpeed, 0.1f, 10f);
                GUILayout.Label("Animation Speed: " + textureAnimationSpeed.ToString("F2") + " fps");
            }

            enableProceduralTextures = GUILayout.Toggle(enableProceduralTextures, "Enable Procedural Textures");
            if (enableProceduralTextures)
            {
                proceduralNoiseScale = GUILayout.HorizontalSlider(proceduralNoiseScale, 0.1f, 10f);
                GUILayout.Label("Noise Scale: " + proceduralNoiseScale.ToString("F2"));
                proceduralColor1 = ColorField("Color 1", proceduralColor1);
                proceduralColor2 = ColorField("Color 2", proceduralColor2);
            }

            if (GUILayout.Button("Apply Texture Modifications"))
            {
                ApplyTextureModifications();
            }

            GUILayout.Space(10);

            GUILayout.Label("Player Texture:");
            changePlayerTexture = GUILayout.Toggle(changePlayerTexture, "Change Player Texture");
            if (changePlayerTexture)
            {
                if (GUILayout.Button("Load Player Texture"))
                {
                    LoadPlayerTexture();
                }
            }

            GUILayout.Space(10);

            GUILayout.Label("Camera Control:");
            if (GUILayout.Button("Refresh Camera List"))
            {
                RefreshCameraList();
            }
            if (allCameras.Count > 0)
            {
                currentCameraIndex = GUILayout.SelectionGrid(currentCameraIndex, allCameras.Select(c => c.name).ToArray(), 1);
            }

            GUILayout.Space(10);

            GUILayout.Label("Looked At Object:");
            GUILayout.Label(lookedAtObjectInfo);

            GUILayout.Space(10);

            GUILayout.Label("Looked At Texture:");
            textureNameScrollPosition = GUILayout.BeginScrollView(textureNameScrollPosition, GUILayout.Height(60));
            GUILayout.Label(lookedAtTextureName);
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button(showHowToUse ? "Hide How to Use" : "Show How to Use"))
            {
                showHowToUse = !showHowToUse;
            }

            if (showHowToUse)
            {
                GUILayout.TextArea(howToUseText, GUILayout.ExpandHeight(true));
            }

            GUILayout.Label("Status: " + statusMessage);

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private Color ColorField(string label, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f);
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f);
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f);
            GUILayout.EndHorizontal();
            return color;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showGUI = !showGUI;
            }

            if (allCameras.Count > currentCameraIndex)
            {
                Camera currentCamera = allCameras[currentCameraIndex];
                if (currentCamera != null)
                {
                    Ray ray = currentCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        GameObject hitObject = hit.collider.gameObject;
                        lookedAtObjectInfo = string.Format("Name: {0}\nPosition: {1}", hitObject.name, hitObject.transform.position);
                        Renderer renderer = hitObject.GetComponent<Renderer>();
                        if (renderer != null && renderer.material.mainTexture != null)
                        {
                            lookedAtTextureName = renderer.material.mainTexture.name;
                        }
                        else
                        {
                            lookedAtTextureName = "No texture found";
                        }
                    }
                    else
                    {
                        lookedAtObjectInfo = "No object in sight";
                        lookedAtTextureName = "No texture found";
                    }
                }
            }

            if (enableRandomTextures)
            {
                UpdateRandomTextures();
            }

            if (enableTextureAnimation)
            {
                UpdateTextureAnimation();
            }

            if (enableProceduralTextures)
            {
                UpdateProceduralTextures();
            }
        }

        private void LoadTexture()
        {
            if (string.IsNullOrEmpty(texturePath))
            {
                statusMessage = "No texture path provided.";
                return;
            }

            Texture2D loadedTexture = LoadTextureFromFile(texturePath);
            if (loadedTexture != null)
            {
                ReplaceTextureOnAllObjects(null, ProcessTexture(loadedTexture));
                statusMessage = "Texture loaded and applied successfully.";
            }
            else
            {
                statusMessage = "Failed to load texture.";
            }
        }

        private void LoadPlayerTexture()
        {
            if (string.IsNullOrEmpty(texturePath))
            {
                statusMessage = "No texture path provided for player texture.";
                return;
            }

            playerTexture = LoadTextureFromFile(texturePath);
            if (playerTexture != null)
            {
                ApplyTextureToPlayer();
                statusMessage = "Player texture loaded and applied successfully.";
            }
            else
            {
                statusMessage = "Failed to load player texture.";
            }
        }

        private void ApplyTextureToPlayer()
        {
            if (playerTexture == null)
            {
                statusMessage = "No player texture loaded.";
                return;
            }

            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                Renderer playerRenderer = playerObject.GetComponent<Renderer>();
                if (playerRenderer != null)
                {
                    playerRenderer.material.mainTexture = ProcessTexture(playerTexture);
                    statusMessage = "Player texture applied successfully.";
                }
                else
                {
                    statusMessage = "Player renderer not found.";
                }
            }
            else
            {
                statusMessage = "Player object not found.";
            }
        }

        private void ApplyTextureModifications()
        {
            if (loadedTextures.Count == 0)
            {
                statusMessage = "No textures loaded to modify.";
                return;
            }

            foreach (var texture in loadedTextures.Values)
            {
                ReplaceTextureOnAllObjects(null, ProcessTexture(texture));
            }

            statusMessage = "Texture modifications applied successfully.";
        }

        private void ImportTexturePack(string packPath)
        {
            if (string.IsNullOrEmpty(packPath) || !File.Exists(packPath))
            {
                statusMessage = "Invalid texture pack path.";
                return;
            }

            try
            {
                Dictionary<string, Texture2D> objectTextures = new Dictionary<string, Texture2D>();

                using (ZipArchive zipArchive = ZipFile.OpenRead(packPath))
                {
                    ZipArchiveEntry configEntry = zipArchive.GetEntry("config.txt");
                    if (configEntry == null)
                    {
                        statusMessage = "config.txt not found in the texture pack.";
                        return;
                    }

                    using (StreamReader streamReader = new StreamReader(configEntry.Open()))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string[] parts = line.Split(new char[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                string objectName = parts[0].Trim();
                                string fileName = parts[1].Trim();

                                ZipArchiveEntry textureEntry = zipArchive.GetEntry(fileName);
                                if (textureEntry != null)
                                {
                                    using (Stream stream = textureEntry.Open())
                                    {
                                        byte[] textureData = new byte[textureEntry.Length];
                                        stream.Read(textureData, 0, textureData.Length);
                                        Texture2D texture = LoadTextureFromBytes(textureData);
                                        if (texture != null)
                                        {
                                            objectTextures[objectName] = texture;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                ApplyTexturesToObjects(objectTextures);
                statusMessage = "Texture pack imported and applied successfully.";
            }
            catch (Exception ex)
            {
                statusMessage = "Error importing texture pack: " + ex.Message;
                Debug.LogError("Texture pack import error: " + ex);
            }
        }
        private void ApplyTexturesToObjects(Dictionary<string, Texture2D> objectTextures)
        {
            foreach (KeyValuePair<string, Texture2D> kvp in objectTextures)
            {
                string objectName = kvp.Key;
                Texture2D newTexture = kvp.Value;

                GameObject[] matchingObjects = UnityEngine.Object.FindObjectsOfType<GameObject>()
                    .Where(obj => obj.name.ToLower().Contains(objectName.ToLower()))
                    .ToArray();

                foreach (GameObject obj in matchingObjects)
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        foreach (string propertyName in renderer.sharedMaterial.GetTexturePropertyNames())
                        {
                            Texture originalTexture = renderer.sharedMaterial.GetTexture(propertyName);
                            if (originalTexture != null)
                            {
                                Texture2D processedTexture = ProcessTexture(newTexture);
                                ReplaceTextureOnAllObjects(originalTexture, processedTexture);
                                break; 
                            }
                        }
                    }
                }
            }
        }

        private void ReplaceTextureOnAllObjects(Texture originalTexture, Texture2D newTexture)
        {
            Renderer[] allRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        Material material = renderer.sharedMaterials[i];
                        if (material != null)
                        {
                            bool materialChanged = false;
                            foreach (string propertyName in material.GetTexturePropertyNames())
                            {
                                if (originalTexture == null || material.GetTexture(propertyName) == originalTexture)
                                {
                                    material.SetTexture(propertyName, newTexture);
                                    materialChanged = true;
                                }
                            }
                            if (materialChanged)
                            {
                                renderer.sharedMaterials[i] = material;
                            }
                        }
                    }
                }
            }
        }

        private Texture2D LoadTextureFromBytes(byte[] fileData)
        {
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                return texture;
            }
            return null;
        }

        private Texture2D LoadTextureFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                statusMessage = "No texture path provided.";
                return null;
            }

            if (!File.Exists(filePath))
            {
                statusMessage = "Texture file not found: " + filePath;
                Debug.LogError("Texture file not found: " + filePath);
                return null;
            }

            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            {
                statusMessage = "Unsupported file type. Use .png, .jpg, or .jpeg";
                return null;
            }

            if (loadedTextures.ContainsKey(filePath))
            {
                return loadedTextures[filePath];
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = LoadTextureFromBytes(fileData);
                if (texture != null)
                {
                    texture.name = Path.GetFileNameWithoutExtension(filePath);
                    loadedTextures[filePath] = texture;
                    return texture;
                }
                Debug.LogError("Failed to load image data into texture.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading texture: " + ex.Message);
                return null;
            }
        }

        private Texture2D ProcessTexture(Texture2D sourceTexture)
        {
            int width = Mathf.RoundToInt(sourceTexture.width * textureScale * qualitySlider);
            int height = Mathf.RoundToInt(sourceTexture.height * textureScale * qualitySlider);
            int repeatedWidth = width * repetitionCount;
            int repeatedHeight = height * repetitionCount;

            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            RenderTexture.active = temporary;
            Graphics.Blit(sourceTexture, temporary);

            Texture2D scaledTexture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            scaledTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            scaledTexture.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temporary);

            Texture2D repeatedTexture = new Texture2D(repeatedWidth, repeatedHeight, TextureFormat.RGBA32, true);
            for (int i = 0; i < repetitionCount; i++)
            {
                for (int j = 0; j < repetitionCount; j++)
                {
                    Graphics.CopyTexture(scaledTexture, 0, 0, 0, 0, width, height, repeatedTexture, 0, 0, j * width, i * height);
                }
            }

            if (pixelationAmount > 1f)
            {
                repeatedTexture = PixelateTexture(repeatedTexture, Mathf.RoundToInt(pixelationAmount));
            }

            repeatedTexture = AdjustDarknessAndContrast(repeatedTexture, darknessAmount, contrastAmount);

            if (blurAmount > 0f)
            {
                repeatedTexture = ApplyBlur(repeatedTexture, blurAmount);
            }

            repeatedTexture.wrapMode = TextureWrapMode.Repeat;
            repeatedTexture.filterMode = FilterMode.Bilinear;
            repeatedTexture.Apply(true);

            return repeatedTexture;
        }
        private Texture2D PixelateTexture(Texture2D source, int pixelSize)
        {
            int width = source.width;
            int height = source.height;
            Texture2D result = new Texture2D(width, height);
            Color[] pixels = source.GetPixels();

            for (int y = 0; y < height; y += pixelSize)
            {
                for (int x = 0; x < width; x += pixelSize)
                {
                    Color averageColor = Color.black;
                    int count = 0;
                    for (int py = 0; py < pixelSize && y + py < height; py++)
                    {
                        for (int px = 0; px < pixelSize && x + px < width; px++)
                        {
                            averageColor += pixels[(y + py) * width + (x + px)];
                            count++;
                        }
                    }
                    averageColor /= count;

                    for (int py = 0; py < pixelSize && y + py < height; py++)
                    {
                        for (int px = 0; px < pixelSize && x + px < width; px++)
                        {
                            result.SetPixel(x + px, y + py, averageColor);
                        }
                    }
                }
            }

            result.Apply();
            return result;
        }

        private Texture2D AdjustDarknessAndContrast(Texture2D source, float darkness, float contrast)
        {
            int width = source.width;
            int height = source.height;
            Color[] pixels = source.GetPixels();
            Color[] result = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color color = pixels[i];
                color = Color.Lerp(color, Color.black, darkness);
                color.r = (color.r - 0.5f) * contrast + 0.5f;
                color.g = (color.g - 0.5f) * contrast + 0.5f;
                color.b = (color.b - 0.5f) * contrast + 0.5f;
                color.r = Mathf.Clamp01(color.r);
                color.g = Mathf.Clamp01(color.g);
                color.b = Mathf.Clamp01(color.b);
                result[i] = color;
            }

            Texture2D adjustedTexture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            adjustedTexture.SetPixels(result);
            adjustedTexture.Apply();
            return adjustedTexture;
        }

        private Texture2D ApplyBlur(Texture2D source, float blurSize)
        {
            int width = source.width;
            int height = source.height;
            Color[] pixels = source.GetPixels();
            Color[] result = new Color[pixels.Length];

            int kernelSize = Mathf.CeilToInt(blurSize * 3f);
            float kernelSum = 0f;
            float[,] kernel = new float[kernelSize * 2 + 1, kernelSize * 2 + 1];

            for (int y = -kernelSize; y <= kernelSize; y++)
            {
                for (int x = -kernelSize; x <= kernelSize; x++)
                {
                    float distance = Mathf.Sqrt(x * x + y * y);
                    float weight = Mathf.Exp(-(distance * distance) / (2f * blurSize * blurSize));
                    kernel[y + kernelSize, x + kernelSize] = weight;
                    kernelSum += weight;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int ky = -kernelSize; ky <= kernelSize; ky++)
                    {
                        for (int kx = -kernelSize; kx <= kernelSize; kx++)
                        {
                            int sampleX = Mathf.Clamp(x + kx, 0, width - 1);
                            int sampleY = Mathf.Clamp(y + ky, 0, height - 1);
                            int sampleIndex = sampleY * width + sampleX;
                            float weight = kernel[ky + kernelSize, kx + kernelSize];
                            r += pixels[sampleIndex].r * weight;
                            g += pixels[sampleIndex].g * weight;
                            b += pixels[sampleIndex].b * weight;
                            a += pixels[sampleIndex].a * weight;
                        }
                    }
                    int resultIndex = y * width + x;
                    result[resultIndex] = new Color(r / kernelSum, g / kernelSum, b / kernelSum, a / kernelSum);
                }
            }

            Texture2D blurredTexture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            blurredTexture.SetPixels(result);
            blurredTexture.Apply();
            return blurredTexture;
        }

        private Texture2D GenerateProceduralTexture()
        {
            int width = 256;
            int height = 256;
            Texture2D proceduralTexture = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float perlinValue = Mathf.PerlinNoise(x * proceduralNoiseScale, y * proceduralNoiseScale);
                    Color pixelColor = Color.Lerp(proceduralColor1, proceduralColor2, perlinValue);
                    proceduralTexture.SetPixel(x, y, pixelColor);
                }
            }

            proceduralTexture.Apply();
            return proceduralTexture;
        }

        private void UpdateRandomTextures()
        {
            if (Time.time - lastRandomTextureTime > randomTextureInterval)
            {
                lastRandomTextureTime = Time.time;
                if (loadedTextures.Count > 0)
                {
                    string randomTexturePath = loadedTextures.Keys.ElementAt(UnityEngine.Random.Range(0, loadedTextures.Count));
                    Texture2D randomTexture = loadedTextures[randomTexturePath];
                    if (randomTexture != null)
                    {
                        ReplaceTextureOnAllObjects(null, ProcessTexture(randomTexture));
                    }
                }
            }
        }

        private void UpdateTextureAnimation()
        {
            if (animationFrames.Count > 0)
            {
                float frameInterval = 1f / textureAnimationSpeed;
                if (Time.time - lastRandomTextureTime > frameInterval)
                {
                    lastRandomTextureTime = Time.time;
                    currentAnimationFrame = (currentAnimationFrame + 1) % animationFrames.Count;
                    ReplaceTextureOnAllObjects(null, ProcessTexture(animationFrames[currentAnimationFrame]));
                }
            }
        }

        private void UpdateProceduralTextures()
        {
            Texture2D proceduralTexture = GenerateProceduralTexture();
            ReplaceTextureOnAllObjects(null, ProcessTexture(proceduralTexture));
        }

        private void OnDisable()
        {
            foreach (var texture in loadedTextures.Values)
            {
                UnityEngine.Object.Destroy(texture);
            }
            loadedTextures.Clear();

            foreach (var frame in animationFrames)
            {
                UnityEngine.Object.Destroy(frame);
            }
            animationFrames.Clear();
        }
    }
}
