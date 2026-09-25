using System;
using System.Collections.Generic;
using UnityEditor;
using Konoha.Campaign;
using Konoha.Character;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Konoha.Editor
{
    // Original civic architecture for the solo campaign. No PvP scene or material is edited.
    internal sealed class CampaignCapitalArt
    {
        private Transform root;
        private Material stone, ivory, bronze, dark, red, green, leaf, paleLeaf, water, paving, flower;
        private Mesh column, dome, roof, arch, feather, frond, pedestal, rim;
        internal Transform Plaza, Majelis, Biro, Garda;
        internal Material Gold => bronze;
        internal Material Ivory => ivory;
        internal Material Red => red;

        internal static CampaignCapitalArt Build()
        {
            if (!AssetDatabase.IsValidFolder(CampaignCapitalMeshes.Folder))
                AssetDatabase.CreateFolder(SpikeProject.Generated, "Capital");
            var art = new CampaignCapitalArt();
            art.ClearSpikeScenery();
            art.CreatePalette();
            art.CreateMeshes();
            art.ConfigureLighting();
            art.root = new GameObject("CapitalEnvironment").transform;
            art.GroundAndStreets();
            art.Monument(new Vector3(0, 0, 1.75f));
            art.CivicHall(new Vector3(0, 0, 20));
            art.Majelis = art.Institution("Majelis Daun", new Vector3(-9, 0, -6), false);
            art.Biro = art.Institution("Biro Prosedur", new Vector3(9, 0, -6), true);
            art.Plaza = art.Objective("Plaza Aspirasi", new Vector3(0, 0, -6.5f), 1.15f);
            art.Garda = art.Objective("Garda Takhta", new Vector3(0, 0, 7.4f), 1.2f);
            foreach (int side in new[] { -1, 1 })
            {
                art.Canal(new Vector3(side * 10, 0, 5.8f));
                art.Statue(new Vector3(side * 18.6f, 0, 9.8f));
                for (int i = 0; i < 4; i++)
                {
                    art.Palm(new Vector3(side * 18.2f, 0, -9 + i * 7), 4.7f + i * 0.35f, i * 31);
                    art.Planter(new Vector3(side * 14, 0, -8 + i * 5.1f), new Vector3(1.4f, 0.55f, 2.3f));
                }
                for (int i = 0; i < 3; i++)
                {
                    art.Palm(new Vector3(side * (5 + i * 5), 0, 15.5f), 5.1f, i * 71);
                    art.Lamp(new Vector3(side * 3.9f, 0, -8.9f + i * 6));
                }
                art.Banner(new Vector3(side * 3.5f, 0, -10.5f), 3.1f);
                art.Banner(new Vector3(side * 15, 0, 9.8f), 3.8f);
                // Skyline wings are beyond collision boundaries, framing the central civic hall.
                for (int i = 0; i < 4; i++)
                    art.Skyline(new Vector3(side * (13 + i * 6.5f), 0, 28 + (i % 2) * 7), 5 + i * 2);
            }
            art.ThroneTrim();
            return art;
        }

        private void ClearSpikeScenery()
        {
            var keep = new HashSet<string> { "KursiSeat", "KursiBack", "KursiLeftArm", "KursiRightArm", "KursiCrown" };
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                // Every Spike arena primitive is a root renderer; player, input and cameras aren't.
                // Destroying geometry also removes its colliders, avoiding invisible old cover.
                if (go.GetComponent<Renderer>() != null && !keep.Contains(go.name))
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void CreatePalette()
        {
            stone = Surface("Andesite", new Color(0.40f, 0.43f, 0.43f), 0.18f);
            ivory = Surface("Limestone", new Color(0.80f, 0.77f, 0.65f), 0.32f);
            bronze = Surface("BrushedBronze", new Color(0.72f, 0.48f, 0.18f), 0.62f, 0.42f);
            dark = Surface("CarvedTimber", new Color(0.17f, 0.21f, 0.19f), 0.23f);
            red = Surface("CrimsonFabric", new Color(0.64f, 0.075f, 0.07f), 0.12f);
            green = Surface("PatinatedRoof", new Color(0.20f, 0.40f, 0.33f), 0.38f, 0.15f);
            leaf = Surface("PalmGreen", new Color(0.12f, 0.31f, 0.10f), 0.2f);
            paleLeaf = Surface("PalmSunlit", new Color(0.29f, 0.46f, 0.12f), 0.23f);
            flower = Surface("Bougainvillea", new Color(0.68f, 0.19f, 0.33f), 0.15f);
            water = Surface("TurquoiseWater", new Color(0.10f, 0.46f, 0.49f), 0.86f, 0.18f);
            paving = Surface("PlazaPaving", new Color(0.63f, 0.64f, 0.60f), 0.29f);
            foreach (var mat in new[] { stone, ivory, dark, green, paving, red, water })
            {
                mat.SetTexture("_BaseMap", Texture(mat == paving ? "Paving" : mat == water ? "Water" : "Grain"));
                if (mat == paving) mat.SetTextureScale("_BaseMap", new Vector2(16, 12));
                EditorUtility.SetDirty(mat);
            }
        }

        private void CreateMeshes()
        {
            column = CampaignCapitalMeshes.Lathe("ProfiledColumn", new[] {
                new Vector2(0,0), new Vector2(.32f,0), new Vector2(.32f,.12f),
                new Vector2(.25f,.18f), new Vector2(.19f,.27f), new Vector2(.16f,2.55f),
                new Vector2(.22f,2.63f), new Vector2(.30f,2.69f), new Vector2(.30f,2.85f), new Vector2(0,2.85f) }, 20);
            dome = CampaignCapitalMeshes.Lathe("CivicRibbedDome", new[] {
                new Vector2(0,0), new Vector2(1,0), new Vector2(1,.08f), new Vector2(.98f,.15f),
                new Vector2(.94f,.31f), new Vector2(.85f,.48f), new Vector2(.71f,.65f),
                new Vector2(.5f,.79f), new Vector2(.27f,.89f), new Vector2(0,.94f) }, 48);
            pedestal = CampaignCapitalMeshes.Lathe("OctagonalMonument", new[] {
                new Vector2(0,0), new Vector2(1.30f,0), new Vector2(1.30f,.18f),
                new Vector2(1.12f,.28f), new Vector2(1.12f,.55f), new Vector2(.86f,.70f),
                new Vector2(.69f,.9f), new Vector2(.39f,5.9f), new Vector2(.54f,6.05f),
                new Vector2(.54f,6.23f), new Vector2(0,6.23f) }, 8);
            roof = CampaignCapitalMeshes.Roof(); arch = CampaignCapitalMeshes.Arch();
            feather = CampaignCapitalMeshes.Feather("SculptedWingFeather", false);
            frond = CampaignCapitalMeshes.Feather("CurvedPalmFrond", true);
            rim = CampaignCapitalMeshes.Ring("ObjectiveRing", .96f, 1);
        }

        private void ConfigureLighting()
        {
            string path = CampaignCapitalMeshes.Folder + "/CampaignURP.asset";
            var source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SpikeProject.Generated + "/SpikeURP.asset");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null) { pipeline = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(pipeline, path); }
            // URP 17 exposes shadow-support switches as read-only properties.
            var serialized = new SerializedObject(pipeline);
            SetBool(serialized, "m_MainLightShadowsSupported", true);
            SetBool(serialized, "m_SoftShadowsSupported", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowDistance = 48f;
            pipeline.mainLightShadowmapResolution = 2048;
            pipeline.shadowCascadeCount = 2;
            pipeline.msaaSampleCount = 2;
            pipeline.supportsHDR = false;
            pipeline.useSRPBatcher = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(0, false);
            EditorUtility.SetDirty(pipeline);
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(48, -38, 0);
            sun.color = new Color(1, .91f, .76f); sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f;
            sun.shadowBias = .035f; sun.shadowNormalBias = .25f;
            RenderSettings.sun = sun;
            GameObject.Find("ArenaFill").GetComponent<Light>().intensity = .18f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.43f, .58f, .72f);
            RenderSettings.ambientEquatorColor = new Color(.42f, .44f, .40f);
            RenderSettings.ambientGroundColor = new Color(.23f, .23f, .18f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.65f, .77f, .79f);
            RenderSettings.fogStartDistance = 43; RenderSettings.fogEndDistance = 105;
            var sky = AssetDatabase.LoadAssetAtPath<Material>(CampaignCapitalMeshes.Folder + "/TropicalSky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, CampaignCapitalMeshes.Folder + "/TropicalSky.mat");
            }
            sky.SetFloat("_AtmosphereThickness", .8f); sky.SetFloat("_Exposure", 1.2f);
            sky.SetColor("_SkyTint", new Color(.47f,.53f,.57f));
            RenderSettings.skybox = sky; EditorUtility.SetDirty(sky);
            Camera.main.clearFlags = CameraClearFlags.Skybox; Camera.main.farClipPlane = 140;
        }

        private static void SetBool(SerializedObject data, string name, bool value)
        {
            var property = data.FindProperty(name);
            if (property == null) throw new InvalidOperationException("URP 17 property missing: " + name);
            property.boolValue = value;
        }

        private void GroundAndStreets()
        {
            Block("Surrounding Konoha Landscape", new Vector3(0,-.65f,10), new Vector3(110,.3f,95), leaf);
            Block("Floor", new Vector3(0,-.35f,0), new Vector3(32,.7f,24), paving, true);
            Block("Civic forecourt", new Vector3(0,-.12f,19), new Vector3(40,.2f,14), stone);
            Block("South boulevard", new Vector3(0,-.11f,-19), new Vector3(14,.2f,14), stone);
            foreach (int side in new[] {-1,1})
            {
                Block(side < 0 ? "WestBoundary" : "EastBoundary", new Vector3(side*16,.42f,0), new Vector3(.6f,.84f,24), stone,true);
                Block(side < 0 ? "SouthBoundary" : "NorthBoundary", new Vector3(0,.42f,side*12), new Vector3(32,.84f,.6f),stone,true);
                Block("Balustrade coping",new Vector3(side*16,.9f,0),new Vector3(.82f,.13f,24),ivory);
                Block("Balustrade coping",new Vector3(0,.9f,side*12),new Vector3(32,.13f,.82f),ivory);
                for (int i=0;i<9;i++)
                {
                    Column(new Vector3(side*16,0,-11+i*2.75f),.38f);
                    Column(new Vector3(-14+i*3.5f,0,side*12),.38f);
                }
            }
            // Thin inlays sit above the floor and do not introduce invisible steps.
            Block("Ceremonial main lane",new Vector3(0,.015f,0),new Vector3(3.5f,.025f,23.2f),ivory);
            Block("Institution approach",new Vector3(0,.03f,-6),new Vector3(23,.025f,2.7f),ivory);
            for(int side=-1;side<=1;side+=2)
                Block("Processional bronze inlay",new Vector3(side*1.72f,.036f,0),new Vector3(.035f,.015f,23),bronze);
            for (int i=0;i<18;i++)
                Block("Paving transverse joint",new Vector3(0,.036f,-11+i*1.25f),new Vector3(3.35f,.014f,.018f),stone);
            var plaza = CampaignCapitalMeshes.Lathe("PlazaSteppedDisc",new[]{ new Vector2(0,0),new Vector2(3.9f,0),
                new Vector2(3.9f,.06f),new Vector2(3.72f,.12f),new Vector2(0,.12f)},64);
            MeshObject("Circular plaza stone",plaza,Vector3.zero,Vector3.one,ivory);
            MeshObject("Plaza concentric carving",CampaignCapitalMeshes.Ring("PlazaRing",3.38f,3.53f),new Vector3(0,.124f,0),Vector3.one,bronze);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;
                var motif=Block("Radial stone inlay",new Vector3(Mathf.Sin(a)*3.05f,.127f,Mathf.Cos(a)*3.05f),new Vector3(.13f,.012f,.36f),dark);
                motif.transform.rotation=Quaternion.Euler(0,i*22.5f,0);
            }
        }

        private void Monument(Vector3 p)
        {
            int firstSurface = root.childCount;
            var baseGo = MeshObject("Monumen Garuda Konoha",pedestal,p,Vector3.one,ivory);
            // Narrow stem collision preserves a walkable ring and throne approach from the south.
            var collider=baseGo.AddComponent<BoxCollider>(); collider.center=new Vector3(0,2.9f,0); collider.size=new Vector3(1.35f,5.8f,1.35f);
            Block("Monument inset relief",p+new Vector3(0,3.25f,-.535f),new Vector3(.29f,4.15f,.035f),bronze);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<7;i++)
                {
                    var wing=MeshObject("Garuda swept feather",feather,p+new Vector3(side*(.20f+i*.19f),6.45f+i*.10f,0),
                        new Vector3(1.5f,1.1f,1.6f+i*.07f),bronze);
                    wing.transform.rotation=Quaternion.Euler(-64+i*4,side*(52+i*7),0);
                }
                Ellipsoid("Garuda wing shoulder",p+new Vector3(side*.55f,6.6f,0),new Vector3(1.25f,.43f,.35f),bronze);
            }
            Ellipsoid("Garuda breast",p+new Vector3(0,6.62f,0),new Vector3(.66f,1.15f,.58f),bronze);
            Ellipsoid("Garuda head",p+new Vector3(0,7.30f,-.08f),new Vector3(.46f,.5f,.45f),bronze);
            var beak=MeshObject("Garuda beak",feather,p+new Vector3(0,7.25f,-.18f),new Vector3(.6f,.55f,.44f),bronze);
            beak.transform.rotation=Quaternion.Euler(14,180,0);
            for(int i=-1;i<=1;i++)
            {
                var tail=MeshObject("Garuda tail",feather,p+new Vector3(i*.16f,6.25f,.10f),new Vector3(.7f,.8f,.85f),bronze);
                tail.transform.rotation=Quaternion.Euler(80,i*18,0);
            }
            // Large lettered plaques use relief geometry; no floating debug label on the landmark.
            Block("Monument dedication plaque",p+new Vector3(0,.62f,-1.02f),new Vector3(.58f,.28f,.045f),dark);
            var monument = baseGo.AddComponent<CampaignMonument>();
            monument.solid = collider;
            monument.player = UnityEngine.Object.FindFirstObjectByType<CharacterMotor>().transform;
            monument.viewCamera = Camera.main;
            int count = root.childCount - firstSurface;
            monument.surfaces = new Renderer[count];
            monument.opaqueMaterials = new Material[count];
            monument.transparentMaterials = new Material[count];
            var variants = new Dictionary<Material, Material>();
            for (int i = 0; i < count; i++)
            {
                var renderer = root.GetChild(firstSurface + i).GetComponent<Renderer>();
                var original = renderer.sharedMaterial;
                if (!variants.TryGetValue(original, out var faded))
                {
                    string path = CampaignCapitalMeshes.Folder + "/" + original.name + "Occluded.mat";
                    faded = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (faded == null) { faded = new Material(original); AssetDatabase.CreateAsset(faded, path); }
                    faded.CopyPropertiesFromMaterial(original);
                    faded.SetFloat("_Surface", 1);
                    faded.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    faded.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    faded.SetFloat("_ZWrite", 0);
                    faded.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    faded.SetOverrideTag("RenderType", "Transparent");
                    faded.renderQueue = (int)RenderQueue.Transparent;
                    EditorUtility.SetDirty(faded);
                    variants.Add(original, faded);
                }
                monument.surfaces[i] = renderer;
                monument.opaqueMaterials[i] = original;
                monument.transparentMaterials[i] = faded;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, 0);
            }
        }

        private void CivicHall(Vector3 p)
        {
            Steps(p+new Vector3(0,0,-5),17,6,.17f,.55f);
            Block("Civic hall podium",p+new Vector3(0,.5f,0),new Vector3(21,1,9),stone);
            Block("Civic hall core",p+new Vector3(0,3.1f,1),new Vector3(17,4.3f,5.5f),ivory);
            Block("Deep colonnade shadow",p+new Vector3(0,3,-2),new Vector3(17,3.7f,.3f),dark);
            for(int i=-5;i<=5;i++) Column(p+new Vector3(i*1.65f,1,-3.4f),1.43f);
            Block("Civic entablature",p+new Vector3(0,5.2f,-.4f),new Vector3(21,.4f,8),ivory);
            Block("Bronze cornice",p+new Vector3(0,5.48f,-.4f),new Vector3(21.3f,.13f,8.2f),bronze);
            MeshObject("Fictional civic dome",dome,p+new Vector3(0,5.6f,0),new Vector3(6.4f,5,5),green);
            MeshObject("Dome drum",column,p+new Vector3(0,5.6f,0),new Vector3(14,.22f,11),ivory);
            Column(p+new Vector3(0,10.2f,0),.45f);
            for(int side=-1;side<=1;side+=2)
            {
                Roof(p+new Vector3(side*9,4.4f,.3f),new Vector3(3.1f,2.1f,4.3f),green);
                Banner(p+new Vector3(side*5.6f,1,-3.65f),4.1f);
            }
            for(int i=-4;i<=4;i++)
                Block("Civic frieze lozenge",p+new Vector3(i*2.1f,5.21f,-4.43f),new Vector3(.26f,.26f,.05f),bronze).transform.rotation=Quaternion.Euler(0,0,45);
        }

        private Transform Institution(string name, Vector3 p, bool archive)
        {
            var objective=Objective(name,p,2.8f);
            Vector3 b=p+new Vector3(0,0,2.9f);
            Block(name+" facade",b+new Vector3(0,1.35f,.8f),new Vector3(5.6f,2.7f,1.2f),ivory,true);
            Block(name+" carved doorway",b+new Vector3(0,1.15f,.17f),new Vector3(1.15f,2.25f,.06f),dark);
            Steps(b+new Vector3(0,0,-1.05f),5.5f,3,.12f,.28f);
            foreach(int side in new[]{-1,1})
            {
                Column(b+new Vector3(side*2.3f,.25f,-.4f),.96f);
                MeshObject(name+" carved window arch",arch,b+new Vector3(side*1.75f,1.65f,.17f),new Vector3(.55f,.65f,.45f),bronze);
                Block(name+" window recess",b+new Vector3(side*1.75f,1.2f,.17f),new Vector3(.85f,1.25f,.05f),dark);
            }
            Block(name+" carved lintel",b+new Vector3(0,3,.1f),new Vector3(6.3f,.3f,3.5f),ivory);
            Roof(b+new Vector3(0,3.25f,.1f),new Vector3(3.4f,2.2f,2.15f),archive?green:red);
            Roof(b+new Vector3(0,4.3f,.1f),new Vector3(2.1f,1.3f,1.3f),archive?green:red);
            if(archive) MeshObject(name+" archive crown",dome,b+new Vector3(0,4.75f,.1f),new Vector3(.72f,1,.72f),bronze);
            Banner(b+new Vector3(-3.15f,0,-.3f),2.7f);
            return objective;
        }

        private void Canal(Vector3 p)
        {
            Block("Canal dark basin",p+new Vector3(0,.006f,0),new Vector3(6.1f,.01f,5.5f),dark);
            var surface=Block("Canal reflective water",p+new Vector3(0,.026f,0),new Vector3(5.7f,.018f,5.1f),water);
            surface.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            foreach(int side in new[]{-1,1})
            {
                Block("Canal carved coping",p+new Vector3(0,.22f,side*2.7f),new Vector3(6.2f,.44f,.24f),ivory,true);
                for(int end=-1;end<=1;end+=2)
                    Block("Canal return coping",p+new Vector3(side*2.95f,.22f,end*1.8f),new Vector3(.24f,.44f,1.7f),ivory,true);
                // Ramp rise .48 over run 1.8 (15 degrees), within the controller's slope limit.
                var ramp=Block("Bridge approach ramp",p+new Vector3(side*3.2f,.26f,0),new Vector3(1.9f,.12f,1.9f),ivory,true);
                ramp.transform.rotation=Quaternion.Euler(0,0,-side*15f);
                Block("Bridge handrail",p+new Vector3(0,1.23f,side*1.08f),new Vector3(4.65f,.12f,.13f),bronze);
                for(int i=-2;i<=2;i++) Column(p+new Vector3(i*1.1f,.5f,side*1.08f),.25f);
            }
            Block("Bridge across reflecting pool",p+new Vector3(0,.43f,0),new Vector3(4.65f,.18f,2.2f),ivory,true);
            MeshObject("Bridge carved arch",arch,p+new Vector3(0,.02f,-1.13f),new Vector3(2.05f,.4f,1),stone);
            MeshObject("Bridge carved arch",arch,p+new Vector3(0,.02f,1.13f),new Vector3(2.05f,.4f,1),stone);
            for(int i=0;i<3;i++)
            {
                var ripple=MeshObject("Fountain ripple",rim,p+new Vector3(1.4f,.044f,1.65f),Vector3.one*(.26f+i*.18f),ivory);
                ripple.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            Ellipsoid("Fountain finial",p+new Vector3(1.4f,.20f,1.65f),new Vector3(.24f,.3f,.24f),bronze);
        }

        private void ThroneTrim()
        {
            foreach(string n in new[]{"KursiSeat","KursiBack","KursiLeftArm","KursiRightArm","KursiCrown"})
                GameObject.Find(n).GetComponent<Renderer>().sharedMaterial=n=="KursiSeat"?red:bronze;
            for(int side=-1;side<=1;side+=2)
                Column(new Vector3(side*.67f,.13f,.35f),.38f);
        }

        private Transform Objective(string name, Vector3 p, float radius)
        {
            var marker=new GameObject(name); marker.transform.SetParent(root); marker.transform.position=p;
            var ring=MeshObject(name+" interaction boundary",rim,p+Vector3.up*.056f,Vector3.one*radius,bronze);
            ring.transform.SetParent(marker.transform,true);
            return marker.transform;
        }

        private void Palm(Vector3 p,float height,float twist)
        {
            // Tapered trunk and pointed bent fronds replace the sphere-tree silhouette.
            var trunk=CampaignCapitalMeshes.Lathe("PalmTrunk",new[]{new Vector2(0,0),new Vector2(.17f,0),new Vector2(.12f,1),new Vector2(0,1)},12);
            MeshObject("Palm tapered trunk",trunk,p,new Vector3(1,height,1),dark);
            for(int i=0;i<10;i++)
            {
                var go=MeshObject("Palm curved frond",frond,p+Vector3.up*height,new Vector3(1.3f,1.35f,2.45f),i%2==0?leaf:paleLeaf);
                go.transform.rotation=Quaternion.Euler(i%2==0?-9:12,twist+i*36,0);
            }
            Ellipsoid("Palm crown",p+Vector3.up*(height-.05f),new Vector3(.48f,.55f,.48f),leaf);
        }

        private void Planter(Vector3 p,Vector3 size)
        {
            Block("Garden planter",p+Vector3.up*(size.y*.5f),size,stone,true);
            Block("Planter coping",p+Vector3.up*size.y,new Vector3(size.x+.14f,.1f,size.z+.14f),ivory);
            for(int i=0;i<3;i++)
            {
                Vector3 q=p+new Vector3(0,size.y+.14f,(i-1)*size.z*.28f);
                Ellipsoid("Garden shrub",q,new Vector3(size.x*.9f,.55f,size.z*.40f),i%2==0?leaf:paleLeaf);
                Ellipsoid("Bougainvillea flowers",q+new Vector3(.16f,.29f,0),new Vector3(.32f,.16f,.35f),flower);
            }
        }

        private void Statue(Vector3 p)
        {
            Block("Guardian pedestal",p+Vector3.up*1.1f,new Vector3(3.2f,2.2f,3),stone);
            Block("Guardian pedestal cornice",p+Vector3.up*2.25f,new Vector3(3.6f,.25f,3.4f),ivory);
            Ellipsoid("Guardian carved torso",p+new Vector3(0,3.7f,0),new Vector3(1.6f,2.1f,.9f),stone);
            Ellipsoid("Guardian head",p+new Vector3(0,5.1f,0),new Vector3(.85f,1.05f,.8f),ivory);
            MeshObject("Guardian ceremonial crown",dome,p+new Vector3(0,5.45f,0),new Vector3(.65f,1.3f,.65f),bronze);
            foreach(int side in new[]{-1,1})
            {
                Ellipsoid("Guardian knee",p+new Vector3(side*.55f,2.75f,-.35f),new Vector3(.7f,.95f,1.1f),stone);
                Ellipsoid("Guardian arm",p+new Vector3(side*.95f,3.7f,-.20f),new Vector3(.53f,1.5f,.55f),ivory);
            }
            Column(p+new Vector3(1.15f,2.4f,-.55f),1.15f);
            Banner(p+new Vector3(-1.5f,0,-1.75f),2.1f);
        }

        private void Skyline(Vector3 p,float height)
        {
            Block("Distant civic tower",p+Vector3.up*(height*.5f),new Vector3(4,height,4),ivory);
            Roof(p+Vector3.up*height,new Vector3(2.7f,2,2.7f),green);
            for(int i=0;i<3;i++)
                Block("Tower recessed arcade",p+new Vector3((i-1)*1.05f,height*.55f,-2.02f),new Vector3(.62f,height*.53f,.05f),dark);
        }

        private void Banner(Vector3 p,float h)
        {
            Column(p,.20f);
            Block("Banner bronze staff",p+Vector3.up*(h*.5f),new Vector3(.07f,h,.07f),bronze);
            Block("Banner crossbar",p+new Vector3(.38f,h,0),new Vector3(.92f,.07f,.07f),bronze);
            // Staggered fabric strips create a folded silhouette and alternating lit normals.
            for(int i=0;i<5;i++)
            {
                float x=.1f+i*.13f;
                var upper=Block("Red civic cloth",p+new Vector3(x,h-.4f,Mathf.Sin(i*1.6f)*.045f),new Vector3(.135f,.74f,.025f),red);
                upper.transform.rotation=Quaternion.Euler(0,i%2==0?16:-16,0);
                var lower=Block("White civic cloth",p+new Vector3(x,h-1.09f,Mathf.Sin(i*1.6f)*.045f),new Vector3(.135f,.63f,.025f),ivory);
                lower.transform.rotation=upper.transform.rotation;
            }
        }

        private void Lamp(Vector3 p)
        {
            Column(p,.54f);
            Ellipsoid("Civic lamp glass",p+Vector3.up*1.67f,new Vector3(.25f,.33f,.25f),ivory);
            MeshObject("Lamp bronze cap",dome,p+Vector3.up*1.8f,new Vector3(.24f,.21f,.24f),bronze);
        }

        private void Steps(Vector3 p,float width,int count,float rise,float run)
        {
            for(int i=0;i<count;i++)
                Block("Carved civic stair",p+new Vector3(0,(i+1)*rise*.5f,i*run),new Vector3(width,(i+1)*rise,run+.02f),ivory,true);
        }
        private void Column(Vector3 p,float scale) => MeshObject("Profiled civic column",column,p,Vector3.one*scale,ivory);
        private void Roof(Vector3 p,Vector3 size,Material mat)
        {
            MeshObject("Tiered Nusantara roof",roof,p,size,mat);
            Block("Roof bronze ridge",p+new Vector3(0,size.y*.65f,0),new Vector3(size.x*.96f,.085f,.10f),bronze);
            for(int side=-1;side<=1;side+=2)
            {
                var finial=MeshObject("Upturned roof finial",feather,p+new Vector3(side*size.x*.98f,.04f,0),new Vector3(.6f,.7f,.8f),bronze);
                finial.transform.rotation=Quaternion.Euler(-65,side*80,0);
            }
        }

        private GameObject Block(string name,Vector3 p,Vector3 scale,Material mat,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); Configure(go,name,p,scale,mat);
            if(!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private GameObject Ellipsoid(string name,Vector3 p,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); Configure(go,name,p,scale,mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private GameObject MeshObject(string name,Mesh mesh,Vector3 p,Vector3 scale,Material mat)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh=mesh; Configure(go,name,p,scale,mat); return go;
        }
        private void Configure(GameObject go,string name,Vector3 p,Vector3 scale,Material mat)
        {
            go.name=name; go.transform.SetParent(root); go.transform.position=p; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
        private Material Surface(string name,Color color,float smooth,float metal=0)
        {
            string path=CampaignCapitalMeshes.Folder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=name;
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metal);
            mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        private Texture2D Texture(string name)
        {
            string path=CampaignCapitalMeshes.Folder+"/"+name+".asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            const int size=256;
            if(tex==null){tex=new Texture2D(size,size,TextureFormat.RGBA32,true);AssetDatabase.CreateAsset(tex,path);}
            tex.name=name;tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Trilinear;tex.anisoLevel=4;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float grain=Mathf.PerlinNoise(x*.21f,y*.21f)*.07f;
                float value=.9f+grain;
                if(name=="Paving")
                {
                    int stagger=(y/128%2)*128;
                    bool joint=y%128<2||(x+stagger)%256<2;
                    value=joint?.65f:.88f+grain+Mathf.PerlinNoise(x*.036f,y*.036f)*.05f;
                }
                if(name=="Water") value=.82f+.12f*Mathf.Sin(x*.17f+Mathf.Sin(y*.08f)*3)+grain;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels);tex.Apply(true,false);EditorUtility.SetDirty(tex);return tex;
        }
    }
}
