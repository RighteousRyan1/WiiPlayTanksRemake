﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics;

// when the room is added!
public static class RoomSceneRenderer {
    // bro there is no construcor stop yapping stupid ide
    public static Model RoomSkyboxScene;

    public static Dictionary<string, Texture2D> RoomSkyboxTextures = [];
    private static List<ModelMesh> _transparentFaces = [];

    public static float Scale;
    public static Vector3 Rotation;
    public static Vector3 Position;

    public static Matrix World;
    public static Matrix View;
    public static Matrix Projection;

    private static Matrix[] _boneTransforms;
    public static void Initialize() {
        //return; // return for now, since .png breaks the UVs vs .jpg
        RoomSkyboxScene = GameResources.GetGameResource<Model>("Assets/models/scene/skybox/room_textureless");
        _boneTransforms = new Matrix[RoomSkyboxScene.Bones.Count];
        GetSkyboxTextures();
    }
    public static void GetSkyboxTextures() {
        foreach (var file in Directory.GetFiles(Path.Combine("Content", "Assets", "models", "scene", "skybox", "textures"))) {
            var fileName = Path.GetFileNameWithoutExtension(file);
            RoomSkyboxTextures.Add(fileName, GameResources.GetGameResource<Texture2D>(file, false, false));
        }
    }
    public static void Render() {
        _transparentFaces.Clear();
        World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X)
            * Matrix.CreateTranslation(Position);
        Scale = 10f;
        Position = new Vector3(-350f, -61.7f, 325f) * Scale;
        View = TankGame.GameView;
        Projection = TankGame.GameProjection;

        RoomSkyboxScene.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        RoomSkyboxScene!.Root.Transform = World;

        foreach (var mesh in RoomSkyboxScene.Meshes) {
            bool drawMe = true;
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = _boneTransforms[mesh.ParentBone.Index];

                effect.View = View;
                effect.Projection = Projection;

                effect.TextureEnabled = true;
                effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/models/scene/skybox/textures/" + GetMeshTexture(mesh));
                effect.Alpha = GetMeshAlpha(mesh);

                if (effect.Alpha < 1f) {
                    _transparentFaces.Add(mesh);
                    drawMe = false;
                }
                /*effect.EnableDefaultLighting();
                effect.DirectionalLight0.SpecularColor = Color.Red.ToVector3();
                effect.DirectionalLight1.SpecularColor = Color.Green.ToVector3();
                effect.DirectionalLight2.SpecularColor = Color.Blue.ToVector3();*/
                effect.SetDefaultGameLighting();
                //effect.SetDefaultGameLighting_Room(new Vector3(0, 0, 1));
                //effect.EnableDefaultLighting();
                /*effect.DirectionalLight0.Direction = Vector3.Down;
                effect.DirectionalLight1.Direction = Vector3.Down.RotateXY(MathHelper.PiOver4);
                effect.DirectionalLight2.Direction = Vector3.Down.RotateXY(-MathHelper.PiOver4);*/

                effect.SpecularPower = 32f;
            }
            if (drawMe)
                mesh.Draw();
        }
        //SceneManager.GameLight.Color = Color.Gray * 0.2f;
        //SceneManager.GameLight.Apply(true);
        /*_transparentFaces.Sort((x, y) =>
        Vector3.Distance(x.ParentBone.Transform.Translation, TankGame.RebirthFreecam.Position)
        .CompareTo(Vector3.Distance(y.ParentBone.Transform.Translation, TankGame.RebirthFreecam.Position)));
        _transparentFaces.ForEach(m => m.Draw());*/
    }

    public static string GetMeshTexture(ModelMesh mesh) {
        return mesh.Name switch {
            "Ceiling" => "ceiling",
            "Chair_Leather" => "leather_red_brown",
            "Chair_Metal" => "metal",
            "Clock_Brass" => "brass",
            "Clock_Face" => "clock_face", // clock_face_0 is also supposed to be here...? not sure what to do about it until BigKitty does something.
            "Clock_Glass" => "glass",
            "Clock_Hand_Hour" => "black",
            "Clock_Hand_Minute" => "black",
            "Clock_Pendulum" => "brass",
            "Clock_Wires" => "brass_wire",
            "Clock_Wood1" => "wood_dark",
            "Clock_Wood2" => "wood_med",
            "Curtains" => "curtains",
            "Desk_Book_page1" => "book1",
            "Desk_Book_Page2" => "book2",
            "Desk_Book_Side" => "book_side",
            "Desk_Lamp" => "brass",
            "Door_Closet" => "wood_white",
            "Door_Closet_Knobs" => "brass",
            "Door_Room_Handle_Brass" => "brass",
            "Door_Room_Handle_Metal" => "metal",
            "Floor" => "floor",
            "Floor_Lamp" => "brass",
            "Floor_Lamp_Bowl" => "lamps",
            "Lamp_Brass" => "brass",
            "Lamp_Shade" => "lamps",
            "Lamp_Wood" => "wood_med",
            "Moulding" => "wood_white",
            "Plant_Brown" => "dirt", // Plant_Pot itself has its own 'color' or something? Check into that.
            "Plant_Green" => "plant",
            "Sphere" => "pano",
            "Table_Top" => "wood_dark", // 'tabletop' will be 'wood_dark' for now, since 'tabletop' is mysteriously missing.
            "Table_Metal" => "metal",
            "Table_Metal_Dark" => "metal_dark",
            "Walls" => "walls",
            "Windows_Glass" => "glass",
            "Windows_Sides" => "window_sides",
            "Shelf" => "wood_dark",
            _ => "metal"
        };
    }
    public static float GetMeshAlpha(ModelMesh mesh) {
        return mesh.Name switch {
            _ when mesh.Name.Contains("glass", StringComparison.InvariantCultureIgnoreCase) => 0.99f,
            _ => 1f
        };
    }
}
