using UnityEngine;

public class NODE
{
    public MAP.BLOCK blockInfo { get; private set; }
    public MAP.ROOM roomInfo { get; private set; }
    public int index;
    public int level;
    public NODE parents;
    public NODE left;
    public NODE right;

    public NODE()
    {
        blockInfo = new MAP.BLOCK();
    }

    public NODE(int _index, int _level, MAP.Vector2D bLeftTop, MAP.Vector2D bRightBot)
    {
        blockInfo = new MAP.BLOCK(bLeftTop, bRightBot);
        index = _index;
        level = _level;
    }

    public bool GenerateRoom()
    {
        if (blockInfo.size.x < MAP.CONTANT.RoomX_Min + MAP.CONTANT.Block_Blank || blockInfo.size.y < MAP.CONTANT.RoomY_Min + MAP.CONTANT.Block_Blank)
            return false;

        roomInfo = new MAP.ROOM();

        int MaxX = blockInfo.size.x > (MAP.CONTANT.RoomX_Max + MAP.CONTANT.Block_Blank) ? MAP.CONTANT.RoomX_Max : (blockInfo.size.x - MAP.CONTANT.Block_Blank);
        int MaxY = blockInfo.size.y > (MAP.CONTANT.RoomY_Max + MAP.CONTANT.Block_Blank) ? MAP.CONTANT.RoomY_Max : (blockInfo.size.y - MAP.CONTANT.Block_Blank);

        MAP.Vector2D roomSize = new MAP.Vector2D(Random.Range(MAP.CONTANT.RoomX_Min, MaxX + 1), Random.Range(MAP.CONTANT.RoomY_Min, MaxY + 1));
        //blank minimum is 2
        MAP.Vector2D blank = new MAP.Vector2D(blockInfo.size - roomSize - 1);
        roomInfo.leftTop.SetVector2D(blockInfo.leftTop.x + Random.Range(1, blank.x + 1), blockInfo.leftTop.y + Random.Range(1, blank.y + 1));
        roomInfo.rightBot.SetVector2D(roomInfo.leftTop + roomSize - 1);

        Debug.Log(index + " / " + blockInfo.leftTop.x + " , " + blockInfo.leftTop.y + " / " + roomSize.x + " / " + roomSize.y);
        //Debug.Log(blockInfo.leftTop.x + " , " + blockInfo.leftTop.y + " / " + blockInfo.rightBot.x + " , " + blockInfo.rightBot.y + " / " + blockInfo.size.x + " , " + blockInfo.size.y);
        return true;
    }
}
