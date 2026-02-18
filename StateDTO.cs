using System.Collections.Generic;

namespace TeRL
{
	public class StateDTO
	{
    public int tick { get; set; }
    public PlayerDTO player { get; set; }
    public InventoryDTO inventory { get; set; }
    public TileWindowDTO tile_window { get; set; }
    public List<EntityDTO> nearby_entities { get; set; }
    public ActionDTO action { get; set; }
	}

	public class NearbyNPCEntry
	{
		public int type { get; set; }
		public float distance { get; set; }
	}

	public class PlayerDTO
	{
		public int tile_x { get; set; }
		public int tile_y { get; set; }
		public float subtile_x { get; set; }
		public float subtile_y { get; set; }
		public float vx { get; set; }
		public float vy { get; set; }
		public int facing { get; set; }
		public bool on_ground { get; set; }
		public int hp { get; set; }
		public int selected_hotbar_slot { get; set; }
	}


	public class InventoryDTO
	{
		public int[] hotbar_item_ids { get; set; }
		public int[] hotbar_stack_counts { get; set; }
	}

	public class TileWindowDTO
	{
		public int width { get; set; }
		public int height { get; set; }
		public int center_offset_x { get; set; }
		public int center_offset_y { get; set; }
		public int channels { get; set; }
		public string[] channel_order { get; set; }
		public int[] tiles_flat { get; set; }
	}

	public class EntityDTO
	{
		public int type_id { get; set; }
		public int tile_x { get; set; }
		public int tile_y { get; set; }
		public float vx { get; set; }
		public float vy { get; set; }
		public int hp { get; set; }
	}

	public class ActionDTO
	{
		public int move_left { get; set; }
		public int move_right { get; set; }
		public int jump { get; set; }
		public int use_item { get; set; }
		public float aim_dx { get; set; }
		public float aim_dy { get; set; }
	}


}
