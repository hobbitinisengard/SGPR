"""Import track2.PMD's original collision polygons into an open Blender scene.

Run with Blender's Python, with the desired track scene already open:
    blender.exe --background track2.blend --python ImportTrack2Collision.py

The imported object's polygon.material_index and face attribute
"pmd_material_index" both equal the original PMD collision material index.
Vertices are converted from PMD's Y-up coordinates to the Z-up convention used
by the companion Blender track scene, so the collision mesh overlays the track.
"""

import colorsys
import math
import os
import struct

import bpy


PMD_PATH = r"C:\Users\bernz\Desktop\SGP Reversed\assets\meshdata\track2.PMD"
OUTPUT_BLEND = r"D:\source\repos\SGPR\Documentation\track2_with_collision.blend"
COLLECTION_NAME = "PMD Collision - track2"
OBJECT_NAME = "track2_PMD_Collision"


def read_u16(data, offset):
    return struct.unpack_from("<H", data, offset)[0]


def read_u32(data, offset):
    return struct.unpack_from("<I", data, offset)[0]


def read_f32(data, offset):
    return struct.unpack_from("<f", data, offset)[0]


def read_collision(path):
    with open(path, "rb") as source:
        data = source.read()
    if len(data) < 328 or data[:9] != b"PMD V1.83":
        raise ValueError("Expected original PMD V1.83 track file")
    if read_u32(data, 0x18) != 1:
        raise ValueError("The PMD file is not a track")

    descriptor = 328 + read_u32(data, 0xA0)
    if descriptor + 40 > len(data):
        raise ValueError("Truncated track collision descriptor")

    def section(header, index, stride):
        base = 328 + read_u32(data, header)
        end = len(data)
        for field in range(0x20, 0xA4, 4):
            candidate = 328 + read_u32(data, field)
            if candidate > base:
                end = min(end, candidate)
        start = base + index * stride
        if base > len(data) or start > end:
            raise ValueError("Invalid track collision section bounds")
        return start, (end - start) // stride

    index_start, index_count = section(0x8C, read_u32(data, descriptor + 4), 2)
    vertex_start, vertex_count = section(0x2C, read_u32(data, descriptor + 8), 16)
    polygon_start, polygon_count = section(0x98, read_u32(data, descriptor + 16), 8)
    material_start, material_count = section(0x94, read_u32(data, descriptor + 20), 4)

    indices = [read_u16(data, index_start + 2 * i) for i in range(index_count)]
    # The source Blender scene uses (PMD X, PMD Z, PMD Y), with Blender Z up.
    vertices = [
        (read_f32(data, vertex_start + 16 * i),
         read_f32(data, vertex_start + 16 * i + 8),
         read_f32(data, vertex_start + 16 * i + 4))
        for i in range(vertex_count)
    ]
    polygons = []
    for i in range(polygon_count):
        at = polygon_start + 8 * i
        vertex_len = data[at + 1]
        first_index = read_u16(data, at + 2)
        material_index = read_u16(data, at + 6)
        if vertex_len < 3 or first_index + vertex_len > len(indices):
            raise ValueError("Invalid PMD collision polygon")
        # Swapping PMD Y/Z reverses handedness, so reverse winding to keep
        # Blender face normals aligned with the source contact normals.
        face = tuple(reversed(indices[first_index:first_index + vertex_len]))
        if any(vertex_index >= len(vertices) for vertex_index in face):
            raise ValueError("PMD collision polygon references an invalid vertex")
        if material_index >= material_count:
            raise ValueError("PMD collision polygon references an invalid material")
        polygons.append((face, material_index))

    materials = []
    for i in range(material_count):
        at = material_start + 4 * i
        flags, grip, rolling, roughness = data[at:at + 4]
        contact_type = flags & 15
        raw_grip = grip / 64.0
        effective_grip = 4.0 if contact_type == 10 else raw_grip
        materials.append({
            "flags": flags,
            "contact_type": contact_type,
            "grip_byte": grip,
            "grip_raw": raw_grip,
            "grip_effective": effective_grip,
            "rolling": rolling,
            "roughness": roughness,
        })
    return vertices, polygons, materials


def make_material(index, values):
    hue = (index * 0.61803398875) % 1.0
    rgb = colorsys.hsv_to_rgb(hue, 0.72, 0.92)
    name = (
        f"PMD_Mat_{index:02d}_"
        f"G{values['grip_byte']}_R{values['rolling']}_Q{values['roughness']}"
    )
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*rgb, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    if shader:
        shader.inputs["Base Color"].default_value = (*rgb, 1.0)
        shader.inputs["Roughness"].default_value = 0.65
    material["PMD material index"] = index
    material["PMD flags"] = values["flags"]
    material["Contact type (low nibble)"] = values["contact_type"]
    material["Grip byte"] = values["grip_byte"]
    material["Grip scalar (raw)"] = values["grip_raw"]
    material["Grip scalar (after type-10 rule)"] = values["grip_effective"]
    material["Rolling byte"] = values["rolling"]
    material["Roughness byte"] = values["roughness"]
    return material


def import_collision():
    vertices, polygons, materials = read_collision(PMD_PATH)

    old_collection = bpy.data.collections.get(COLLECTION_NAME)
    if old_collection:
        for obj in list(old_collection.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old_collection)

    collection = bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(collection)

    mesh = bpy.data.meshes.new(OBJECT_NAME + "Mesh")
    mesh.from_pydata(vertices, [], [face for face, _ in polygons])
    mesh.update()
    obj = bpy.data.objects.new(OBJECT_NAME, mesh)
    collection.objects.link(obj)

    imported_materials = [make_material(i, values) for i, values in enumerate(materials)]
    for material in imported_materials:
        mesh.materials.append(material)
    for polygon, (_, material_index) in zip(mesh.polygons, polygons):
        polygon.material_index = material_index

    attribute = mesh.attributes.new("pmd_material_index", "INT", "FACE")
    for polygon, (_, material_index) in zip(mesh.polygons, polygons):
        attribute.data[polygon.index].value = material_index

    obj["Source PMD"] = os.path.basename(PMD_PATH)
    obj["Source collision polygons"] = len(polygons)
    obj["Source collision vertices"] = len(vertices)
    obj["Material index field"] = "PMD collision polygon record, uint16 at byte offset +6"
    obj.show_wire = True
    obj.show_all_edges = True

    print("Imported PMD collision mesh:", OBJECT_NAME)
    print("Vertices:", len(vertices), "Polygons:", len(polygons), "Materials:", len(materials))
    mins = tuple(min(v[axis] for v in vertices) for axis in range(3))
    maxs = tuple(max(v[axis] for v in vertices) for axis in range(3))
    print("Blender-space bounds:", mins, maxs)
    print("Scene mesh objects:")
    for scene_obj in bpy.context.scene.objects:
        if scene_obj.type == "MESH" and scene_obj != obj:
            print(" ", scene_obj.name, "loc=", tuple(scene_obj.location), "dims=", tuple(scene_obj.dimensions))

    os.makedirs(os.path.dirname(OUTPUT_BLEND), exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_BLEND)
    print("Saved:", OUTPUT_BLEND)


import_collision()
