using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //TEMP
    public GameObject Empty;
    public GameObject Room_Wall;
    public GameObject Room_Floor;
    public GameObject Room_Door;
    public GameObject Passage_Floor;
    public GameObject Passage_Wall;

    MAP.SPACE[][] Map;

    NODE Root;
    int Depth;
    public int RoomNum;
    public List<NODE> RoomList;

    void Awake()
    {
        Map = new MAP.SPACE[MAP.CONTANT.Map_Y][];
        for (int y = 0; y < MAP.CONTANT.Map_Y; ++y)
        {
            Map[y] = new MAP.SPACE[MAP.CONTANT.Map_X];
            for (int x = 0; x < MAP.CONTANT.Map_X; ++x)
                Map[y][x] = MAP.SPACE.EMPTY;
        }

        SetTreeDepth();
        Root = new NODE(1, 0, new MAP.Vector2D(0, 0), new MAP.Vector2D(MAP.CONTANT.Map_X - 1, MAP.CONTANT.Map_Y - 1));
    }

    void SetTreeDepth()
    {
        int depth = -1;
        int compareValue = 1;
        int temp = 0;
        while (true)
        {
            temp = compareValue - RoomNum;
            if (temp >= 0)
                break;

            compareValue = compareValue << 1;
            depth += 1;
        }
    }

    // Use this for initialization
    void Start()
    {
        if (GenerateTree())
            GenerateMap();
        else
            Debug.Log("Create Fail");
    }

    bool GenerateTree()
    {
        if (Root == null)
            return false;

        int roomNum = 1;
        Queue<NODE> bfsQueue = new Queue<NODE>();
        Stack<NODE> r_bfsStack = new Stack<NODE>();
        NODE curNode = Root;

        while (roomNum <= RoomNum)
        {
            if (!DivideBlock(curNode))
            {
                Debug.Log("Fail Divide Block");
                return false;
            }

            bfsQueue.Enqueue(curNode.left);
            bfsQueue.Enqueue(curNode.right);
            r_bfsStack.Push(curNode.left);
            r_bfsStack.Push(curNode.right);

            SetBlockInfo(curNode.left.blockInfo);

            curNode = bfsQueue.Dequeue();
            roomNum = curNode.index + 1;
        }

        //Generate Room
        if (!GenerateRoom(r_bfsStack.ToArray()))
        {
            Debug.Log("Fail Create Room");
            return false;
        }
        SetRoomInfo();

        if(!ConnetRoom(r_bfsStack))
        {
            Debug.Log("Fail Connet Room");
            return false;
        }
        
        return true;
    }

    private bool DivideBlock(NODE curNode)
    {
        //Set Basic Info
        curNode.left = new NODE();
        curNode.left.parents = curNode;
        curNode.left.index = curNode.index * 2;
        curNode.left.level = curNode.level + 1;
        curNode.left.blockInfo.leftTop.SetVector2D(curNode.blockInfo.leftTop);

        curNode.right = new NODE();
        curNode.right.parents = curNode;
        curNode.right.index = (curNode.index * 2) + 1;
        curNode.right.level = curNode.level + 1;
        curNode.right.blockInfo.rightBot.SetVector2D(curNode.blockInfo.rightBot);

        //Divide Random Coodinate
        int sizeX = curNode.blockInfo.rightBot.x - curNode.blockInfo.leftTop.x;
        int sizeY = curNode.blockInfo.rightBot.y - curNode.blockInfo.leftTop.y;

        //Set Random Divide Direction
        MAP.DIVIDE dir = MAP.DIVIDE.NONE;

        int tryNum = 0;
        while (tryNum < 10000)
        {
            dir = (MAP.DIVIDE)Random.Range(0, 2);
            if (sizeX * 0.6f > sizeY)
                dir = MAP.DIVIDE.VERTICAL;
            if (sizeY * 0.6f > sizeX)
                dir = MAP.DIVIDE.HORIZONTAL;

            tryNum += 1;
            switch (dir)
            {
                case MAP.DIVIDE.VERTICAL:
                    int randX = (int)Random.Range(sizeX * 0.4f, sizeX * 0.6f);
                    // +2 => Passage Size
                    // +3 => Passage Size + Block Line
                    if (randX < MAP.CONTANT.RoomX_Min + MAP.CONTANT.Block_Blank || sizeX - randX < MAP.CONTANT.RoomX_Min + MAP.CONTANT.Block_Blank + 1)
                        continue;

                    curNode.left.blockInfo.rightBot.SetVector2D(curNode.blockInfo.leftTop.x + randX, curNode.blockInfo.rightBot.y);
                    curNode.right.blockInfo.leftTop.SetVector2D(curNode.blockInfo.leftTop.x + randX + 2, curNode.blockInfo.leftTop.y);
                    tryNum = 0;
                    break;
                case MAP.DIVIDE.HORIZONTAL:
                    int randY = (int)Random.Range(sizeY * 0.4f, sizeY * 0.6f);
                    if (randY < MAP.CONTANT.RoomY_Min + 2 || sizeY - randY < MAP.CONTANT.RoomY_Min + 3)
                        continue;

                    curNode.left.blockInfo.rightBot.SetVector2D(curNode.blockInfo.rightBot.x, curNode.blockInfo.leftTop.y + randY);
                    curNode.right.blockInfo.leftTop.SetVector2D(curNode.blockInfo.leftTop.x, curNode.blockInfo.leftTop.y + randY + 2);
                    tryNum = 0;
                    break;
            }
            break;
        }
        if (dir == MAP.DIVIDE.NONE)
            return false;

        curNode.left.blockInfo.divideDir = dir;
        curNode.right.blockInfo.divideDir = dir;
        curNode.left.blockInfo.SetBlockSize();
        curNode.right.blockInfo.SetBlockSize();

        return true;
    }

    public bool GenerateRoom(NODE[] blockArr)
    {
        RoomList = new List<NODE>();

        for(int i = 0; i < RoomNum; ++i)
        {
            if (!blockArr[i].GenerateRoom())
                return false;

            RoomList.Add(blockArr[i]);
        }
        return true;
    }

    bool ConnetRoom(Stack<NODE> r_bfsStack)
    {
        NODE rightNode = null;
        NODE lNode = null;
        NODE rNode = null;
        List<NODE> roomList_left;
        List<NODE> roomList_right;
        NODE lParentsNode = null;
        NODE rParentsNode = null;

        int nodeNum = 2;
        if (RoomNum % 2 == 1)
            nodeNum = 1;
        while (r_bfsStack.Count > 0)
        {
            rightNode = r_bfsStack.Pop();
            roomList_right = GetRoomList(rightNode);
            roomList_left = GetRoomList(r_bfsStack.Pop());

            //Check Minimum Distance
            float minDist = float.MaxValue;
            float curDist;
            //Debug.Log("------");
            for (int rIndex = 0; rIndex < roomList_right.Count; ++rIndex)
            {
                for (int lIndex = 0; lIndex < roomList_left.Count; ++lIndex)
                {
                    if (IsClose(roomList_left[lIndex].blockInfo, roomList_right[rIndex].blockInfo))
                    {
                        curDist = (float)MAP.Vector2D.Distance(roomList_right[rIndex].roomInfo.GetCenterPosition(), roomList_left[lIndex].roomInfo.GetCenterPosition());
                        if (curDist < minDist)
                        {
                            minDist = curDist;
                            lNode = roomList_left[lIndex];
                            rNode = roomList_right[rIndex];
                        }
                    }
                }
            }
            //Connet Select Room
            GeneratePassage(lNode, rNode, rightNode.blockInfo.divideDir);

            //트리의 깊이가 2이상인 경우만 추가 통로 생성
            if (rightNode.index > 3)
            {
                lNode = null;
                rNode = null;
                if (nodeNum == 2)
                {
                    rParentsNode = rightNode.parents;
                    --nodeNum;
                }
                else if (nodeNum == 1)
                {
                    lParentsNode = rightNode.parents;
                    if (rParentsNode != null && lParentsNode != null)
                    {
                        //연결하려는 두 서브 트리의 루트를 나눈 방향과 두 루트의 부모 노드를 나눈 방향이 달라야 한다
                        if (!(rParentsNode.blockInfo.divideDir == rParentsNode.right.blockInfo.divideDir &&
                            rParentsNode.right.blockInfo.divideDir == lParentsNode.right.blockInfo.divideDir))
                        {
                            roomList_right = GetRoomList(rParentsNode);
                            roomList_left = GetRoomList(lParentsNode);
                            minDist = float.MaxValue;
                            float sMinDist = float.MaxValue;
                            NODE lNode_s = null;
                            NODE rNode_s = null;
                            if (lParentsNode != null && rParentsNode != null)
                            {
                                for (int rIndex = 0; rIndex < roomList_right.Count; ++rIndex)
                                {
                                    for (int lIndex = 0; lIndex < roomList_left.Count; ++lIndex)
                                    {
                                        if (IsClose(roomList_left[lIndex].blockInfo, roomList_right[rIndex].blockInfo))
                                        {
                                            curDist = (float)MAP.Vector2D.Distance(roomList_right[rIndex].roomInfo.GetCenterPosition(), roomList_left[lIndex].roomInfo.GetCenterPosition());
                                            if (curDist < sMinDist)
                                            {
                                                if (curDist < minDist)
                                                {
                                                    sMinDist = minDist;
                                                    minDist = curDist;
                                                    lNode_s = lNode;
                                                    rNode_s = rNode;
                                                    lNode = roomList_left[lIndex];
                                                    rNode = roomList_right[rIndex];
                                                }
                                                else
                                                {
                                                    sMinDist = curDist;
                                                    lNode_s = roomList_left[lIndex];
                                                    rNode_s = roomList_right[rIndex];
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (lNode_s != null && rNode_s != null)
                                GeneratePassage(lNode_s, rNode_s, rParentsNode.blockInfo.divideDir);
                        }
                    }
                    nodeNum = 2;
                    lParentsNode = null;
                    rParentsNode = null;
                }
            }
        }
        return true;
    }

    public bool IsClose(MAP.BLOCK b1, MAP.BLOCK b2)
    {
        MAP.Vector2D b1_RB = b1.rightBot + 2;
        MAP.Vector2D b2_LT = b2.leftTop - 2;
        //Left Right
        if (b1_RB.x - b2_LT.x == 2)
            return true;
        //Up Down
        if (b1_RB.y - b2_LT.y == 2)
            return true;

        return false;
    }

    public void GeneratePassage(NODE lNode, NODE rNODE, MAP.DIVIDE divideDir)
    {
        MAP.DIRECTION lDir = MAP.DIRECTION.NONE;
        MAP.DIRECTION rDir = MAP.DIRECTION.NONE;

        switch (divideDir)
        {
            case MAP.DIVIDE.VERTICAL:
                lDir = MAP.DIRECTION.LEFT;
                rDir = MAP.DIRECTION.RIGHT;
                break;
            case MAP.DIVIDE.HORIZONTAL:
                lDir = MAP.DIRECTION.DOWN;
                rDir = MAP.DIRECTION.UP;
                break;
        }

        if (lDir != MAP.DIRECTION.NONE)
        {
            //Debug.Log(lDir + " , " + rDir);
            ConnectRoom(lNode.roomInfo, rNODE.roomInfo, lDir, rDir);
        }
    }

    void ConnectRoom(MAP.ROOM lRoom, MAP.ROOM rRoom, MAP.DIRECTION lDir, MAP.DIRECTION rDir)
    {
        bool createLDoor = lRoom.CreateDoor(lDir);
        bool createRDoor = rRoom.CreateDoor(rDir);

        MAP.Vector2D lDoor = lRoom.GetDoorPosition(lDir);
        MAP.Vector2D rDoor = rRoom.GetDoorPosition(rDir);
        Map[lDoor.y][lDoor.x] = MAP.SPACE.ROOM_DOOR;
        Map[rDoor.y][rDoor.x] = MAP.SPACE.ROOM_DOOR;

        //직선인 경우
        if (lDoor.y == rDoor.y)
        {
            int x = 1;
            do
            {
                Map[lDoor.y - 1][lDoor.x + x] = Map[lDoor.y - 1][lDoor.x + x] | MAP.SPACE.PASSAGE_WALL;
                Map[lDoor.y][lDoor.x + x] = Map[lDoor.y][lDoor.x + x] | MAP.SPACE.PASSAGE_FLOOR;
                Map[lDoor.y + 1][lDoor.x + x] = Map[lDoor.y + 1][lDoor.x + x] | MAP.SPACE.PASSAGE_WALL;
                ++x;
            } while (lDoor.x + x != rDoor.x);
        }

        else if (lDoor.x == rDoor.x)
        {
            int y = 1;
            do
            {
                Map[lDoor.y + y][lDoor.x - 1] = Map[lDoor.y + y][lDoor.x - 1] | MAP.SPACE.PASSAGE_WALL;
                Map[lDoor.y + y][lDoor.x] = Map[lDoor.y + y][lDoor.x] | MAP.SPACE.PASSAGE_FLOOR;
                Map[lDoor.y + y][lDoor.x + 1] = Map[lDoor.y + y][lDoor.x + 1] | MAP.SPACE.PASSAGE_WALL;
                ++y;
            } while (lDoor.y + y != rDoor.y);
        }
        //직선이 아닌 경우
        else
        {
            int pos = 0;
            if (createLDoor)
                pos = CreatePassage(lDoor, lDir);
            if (createRDoor)
                pos = CreatePassage(rDoor, rDir);

            ConnectPassage(pos, lDoor, rDoor, lDir);
        }
    }

    int CreatePassage(MAP.Vector2D doorPos, MAP.DIRECTION direction)
    {
        int dir = 0;
        int coord = 0;
        MAP.SPACE curSpace;
        if (direction == MAP.DIRECTION.UP || direction == MAP.DIRECTION.DOWN)
        {
            dir = direction == MAP.DIRECTION.UP ? -1 : 1;
            while (true)
            {
                ++coord;
                curSpace = Map[doorPos.y + (coord * dir)][doorPos.x];

                Map[doorPos.y + (coord * dir)][doorPos.x - 1] = Map[doorPos.y + (coord * dir)][doorPos.x - 1] | MAP.SPACE.PASSAGE_WALL;
                Map[doorPos.y + (coord * dir)][doorPos.x] = Map[doorPos.y + (coord * dir)][doorPos.x] | MAP.SPACE.PASSAGE_FLOOR;
                Map[doorPos.y + (coord * dir)][doorPos.x + 1] = Map[doorPos.y + (coord * dir)][doorPos.x + 1] | MAP.SPACE.PASSAGE_WALL;

                if ((curSpace & MAP.SPACE.BLOCK) == MAP.SPACE.BLOCK)
                    break;

                if ((curSpace & MAP.SPACE.PASSAGE_WALL) == MAP.SPACE.PASSAGE_WALL)
                {
                    Map[doorPos.y + (coord * dir)][doorPos.x] = MAP.SPACE.PASSAGE_FLOOR;
                    ++coord;
                    break;
                }
            }
            coord = doorPos.y + (coord * dir);
        }
        else
        {
            dir = direction == MAP.DIRECTION.RIGHT ? -1 : 1;
            while (true)
            {
                ++coord;
                curSpace = Map[doorPos.y][doorPos.x + (coord * dir)];

                Map[doorPos.y - 1][doorPos.x + (coord * dir)] = Map[doorPos.y - 1][doorPos.x + (coord * dir)] | MAP.SPACE.PASSAGE_WALL;
                Map[doorPos.y][doorPos.x + (coord * dir)] = Map[doorPos.y][doorPos.x + (coord * dir)] | MAP.SPACE.PASSAGE_FLOOR;
                Map[doorPos.y + 1][doorPos.x + (coord * dir)] = Map[doorPos.y + 1][doorPos.x + (coord * dir)] | MAP.SPACE.PASSAGE_WALL;

                if ((curSpace & MAP.SPACE.BLOCK) == MAP.SPACE.BLOCK)
                    break;

                if ((curSpace & MAP.SPACE.PASSAGE_WALL) == MAP.SPACE.PASSAGE_WALL)
                {
                    Map[doorPos.y][doorPos.x + (coord * dir)] = MAP.SPACE.PASSAGE_FLOOR;
                    ++coord;
                    break;
                }
            }
            coord = doorPos.x + (coord * dir);
        }
        return coord;
    }

    void ConnectPassage(int pos, MAP.Vector2D lDoor, MAP.Vector2D rDoor, MAP.DIRECTION lDir)
    {
        int coord = 0;
        int small = 0;
        int large = 0;
        //왼쪽 문 방향이 아래 = 횡으로  연결
        if (lDir == MAP.DIRECTION.DOWN)
        {
            small = lDoor.x < rDoor.x ? lDoor.x : rDoor.x;
            large = lDoor.x > rDoor.x ? lDoor.x : rDoor.x;

            if ((Map[pos - 1][small + coord] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                Map[pos - 1][small + coord] = MAP.SPACE.PASSAGE_WALL;

            if ((Map[pos + 1][small + coord] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                Map[pos + 1][small + coord] = MAP.SPACE.PASSAGE_WALL;

            Map[pos][small + coord] = Map[pos][small + coord] | MAP.SPACE.PASSAGE_FLOOR;
            ++coord;

            while (true)
            {
                if (small + coord == large)
                {
                    if ((Map[pos - 1][small + coord] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                        Map[pos - 1][small + coord] = (Map[pos - 1][small + coord] | MAP.SPACE.PASSAGE_WALL);

                    if ((Map[pos + 1][small + coord] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                        Map[pos + 1][small + coord] = (Map[pos + 1][small + coord] | MAP.SPACE.PASSAGE_WALL);

                    Map[pos][small + coord] = Map[pos][small + coord] | MAP.SPACE.PASSAGE_FLOOR;
                    break;
                }

                Map[pos - 1][small + coord] = Map[pos - 1][small + coord] | MAP.SPACE.PASSAGE_WALL;
                Map[pos][small + coord] = Map[pos][small + coord] | MAP.SPACE.PASSAGE_FLOOR;
                Map[pos + 1][small + coord] = Map[pos + 1][small + coord] | MAP.SPACE.PASSAGE_WALL;
                ++coord;
            }
        }
        else
        {
            small = lDoor.y < rDoor.y ? lDoor.y : rDoor.y;
            large = lDoor.y > rDoor.y ? lDoor.y : rDoor.y;

            if ((Map[small + coord][pos - 1] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                Map[small + coord][pos - 1] = Map[small + coord][pos - 1] | MAP.SPACE.PASSAGE_WALL;

            if ((Map[small + coord][pos + 1] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                Map[small + coord][pos + 1] = Map[small + coord][pos + 1] | MAP.SPACE.PASSAGE_WALL;

            Map[small + coord][pos] = Map[small + coord][pos] | MAP.SPACE.PASSAGE_FLOOR;
            ++coord;

            while (true)
            {
                if (small + coord == large)
                {
                    if ((Map[small + coord][pos - 1] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                        Map[small + coord][pos - 1] = Map[small + coord][pos - 1] | MAP.SPACE.PASSAGE_WALL;

                    if ((Map[small + coord][pos + 1] & MAP.SPACE.PASSAGE_FLOOR) != MAP.SPACE.PASSAGE_FLOOR)
                        Map[small + coord][pos + 1] = Map[small + coord][pos + 1] | MAP.SPACE.PASSAGE_WALL;

                    Map[small + coord][pos] = Map[small + coord][pos] | MAP.SPACE.PASSAGE_FLOOR;
                    break;
                }

                Map[small + coord][pos - 1] = Map[small + coord][pos - 1] | MAP.SPACE.PASSAGE_WALL;
                Map[small + coord][pos] = Map[small + coord][pos] | MAP.SPACE.PASSAGE_FLOOR;
                Map[small + coord][pos + 1] = Map[small + coord][pos + 1] | MAP.SPACE.PASSAGE_WALL;
                ++coord;
            }
        }
    }

    List<NODE> GetRoomList(NODE parentsNode)
    {
        List<NODE> roomList = new List<NODE>();
        //Current Node is TerminalNode
        if (parentsNode.roomInfo != null)
            roomList.Add(parentsNode);
        else
        {
            Queue<NODE> bfsQueue = new Queue<NODE>();
            bfsQueue.Enqueue(parentsNode);
            NODE curNode;
            while (bfsQueue.Count > 0)
            {
                curNode = bfsQueue.Dequeue();
                if (curNode.roomInfo != null)
                    roomList.Add(curNode);
                else
                {
                    bfsQueue.Enqueue(curNode.left);
                    bfsQueue.Enqueue(curNode.right);
                }
            }
        }
        return roomList;
    }

    void SetBlockInfo(MAP.BLOCK bLeft)
    {
        switch(bLeft.divideDir)
        {
            case MAP.DIVIDE.VERTICAL:
                for(int y = bLeft.leftTop.y; y <= bLeft.rightBot.y; ++y)
                    Map[y][bLeft.rightBot.x + 1] = MAP.SPACE.BLOCK;
                break;
            case MAP.DIVIDE.HORIZONTAL:
                for (int x = bLeft.leftTop.x; x <= bLeft.rightBot.x; ++x)
                    Map[bLeft.rightBot.y + 1][x] = MAP.SPACE.BLOCK;
                break;
        }
    }

    void SetRoomInfo()
    {
        for (int room = 0; room < RoomList.Count; ++room)
        {
            for (int x = RoomList[room].roomInfo.leftTop.x; x < RoomList[room].roomInfo.rightBot.x; ++x)
            {
                Map[RoomList[room].roomInfo.leftTop.y][x] = MAP.SPACE.ROOM_WALL;
                Map[RoomList[room].roomInfo.rightBot.y][x] = MAP.SPACE.ROOM_WALL;
            }
            for (int y = RoomList[room].roomInfo.leftTop.y; y <= RoomList[room].roomInfo.rightBot.y; ++y)
            {
                Map[y][RoomList[room].roomInfo.leftTop.x] = MAP.SPACE.ROOM_WALL;
                Map[y][RoomList[room].roomInfo.rightBot.x] = MAP.SPACE.ROOM_WALL;
            }

            for (int y = RoomList[room].roomInfo.leftTop.y + 1; y < RoomList[room].roomInfo.rightBot.y; ++y)
            {
                for (int x = RoomList[room].roomInfo.leftTop.x + 1; x < RoomList[room].roomInfo.rightBot.x; ++x)
                {
                    Map[y][x] = MAP.SPACE.ROOM_FLOOR;
                }
            }
        }
    }

    void GenerateMap()
    {
        for (int y = 0; y < MAP.CONTANT.Map_Y; ++y)
        {
            for (int x = 0; x < MAP.CONTANT.Map_X; ++x)
            {
                switch (Map[y][x])
                {
                    case MAP.SPACE.BLOCK:
                        break;
                    case MAP.SPACE.EMPTY:
                        GenerateTile(x, y, Empty);
                        break;
                    case MAP.SPACE.ROOM_FLOOR:
                        GenerateTile(x, y, Room_Floor);
                        break;
                    case MAP.SPACE.ROOM_WALL:
                        GenerateTile(x, y, Room_Wall);
                        break;
                    case MAP.SPACE.ROOM_DOOR:
                        GenerateTile(x, y, Room_Door);
                        break;
                    case MAP.SPACE.PASSAGE_FLOOR:
                        GenerateTile(x, y, Passage_Floor);
                        break;
                    case MAP.SPACE.PASSAGE_WALL:
                        GenerateTile(x, y, Passage_Wall);
                        break;
                    default:
                        if ((Map[y][x] & MAP.SPACE.PASSAGE_FLOOR) == MAP.SPACE.PASSAGE_FLOOR)
                            GenerateTile(x, y, Passage_Floor);

                        else if ((Map[y][x] & MAP.SPACE.PASSAGE_WALL) == MAP.SPACE.PASSAGE_WALL)
                            GenerateTile(x, y, Passage_Wall);
                        break;
                }
            }
        }
    }

    void GenerateTile(int x, int y, GameObject tile)
    {
        Instantiate(tile, new Vector3(x * MAP.CONTANT.Tile_X, 0, y * MAP.CONTANT.Tile_Y), Quaternion.identity);
    }
}
