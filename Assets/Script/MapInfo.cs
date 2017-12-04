using UnityEngine;

namespace MAP
{
    public struct CONTANT
    {
        public const int Tile_X = 1;
        public const int Tile_Y = 1;

        public const int Map_X = 50;
        public const int Map_Y = 50;

        public const int RoomX_Min = 7;
        public const int RoomY_Min = 7;

        public const int RoomX_Max = 15;
        public const int RoomY_Max = 15;

        public const int Block_Blank = 2;
    }

    public enum SPACE
    {
        EMPTY = 0,
        BLOCK = 1,
        ROOM_WALL = 1 << 1,
        ROOM_FLOOR = 1 << 2,
        ROOM_DOOR = 1 << 3,
        PASSAGE_WALL = 1 << 4,
        PASSAGE_FLOOR = 1 << 5,
    };

    public enum DIVIDE
    {
        NONE = -1,
        VERTICAL,
        HORIZONTAL,
    }

    public enum DIRECTION
    {
        NONE = -1,
        UP,
        RIGHT,
        DOWN,
        LEFT,
    }

    public enum PASSAGE
    {
        NONE,
        EXIST,
    }

    public class Vector2D
    {
        public int x { get; set; }
        public int y { get; set; }

        public Vector2D() { x = -1; y = -1; }
        public Vector2D(int _x, int _y) { x = _x; y = _y; }
        public Vector2D(Vector2D vector) { x = vector.x; y = vector.y; }

        public void SetVector2D(int _x, int _y) {  x = _x;  y = _y; }
        public void SetVector2D(Vector2D vector) {  x = vector.x;    y = vector.y;  }

        public void SetNullClass() { x = -1; y = -1; }
        public bool NullClass() { return x == -1 && y == -1 ? true : false; }

        public double Magnitude() { return Mathf.Sqrt(x * x + y * y); }
        public static double Distance(Vector2D v1, Vector2D v2) { return (v2 - v1).Magnitude(); }

        public static Vector2D operator +(Vector2D v1, Vector2D v2) { return new Vector2D(v1.x + v2.x, v1.y + v2.y); }
        public static Vector2D operator -(Vector2D v1, Vector2D v2) { return new Vector2D(v1.x - v2.x, v1.y - v2.y); }
        public static Vector2D operator +(Vector2D v1, int v) { return new Vector2D(v1.x + v, v1.y + v); }
        public static Vector2D operator -(Vector2D v1, int v) { return new Vector2D(v1.x - v, v1.y - v); }
        public static Vector2D operator *(Vector2D v1, int divide) { return new Vector2D((int)(v1.x * divide), (int)(v1.y * divide)); }
        public static Vector2D operator /(Vector2D v1, int divide) { return new Vector2D((int)(v1.x / divide), (int)(v1.y / divide)); }
    }

    public class BLOCK
    {
        public DIVIDE divideDir { get; set; }
        public MAP.Vector2D leftTop { get; set; }
        public MAP.Vector2D rightBot { get; set; }
        public MAP.Vector2D size {  get; private set; }

        public BLOCK()
        {
            leftTop = new MAP.Vector2D();
            rightBot = new MAP.Vector2D();
        }
        public BLOCK(Vector2D _leftTop, Vector2D _rightBot)
        {
            leftTop = _leftTop;
            rightBot = _rightBot;
            SetBlockSize();
        }
        public void SetBlockSize()
        {
            size = new Vector2D();
            size.SetVector2D(rightBot.x - leftTop.x + 1, rightBot.y - leftTop.y + 1);
            //Debug.Log(leftTop.x + " , " + leftTop.y + " / " + rightBot.x + " , " + rightBot.y + " / " + size.x + " , " + size.y);
        }
    }

    public class ROOM
    {
        public MAP.Vector2D leftTop { get; private set; }
        public MAP.Vector2D rightBot{ get; private set; }
        public PASSAGE[] passageInfo { get; private set; }
        public MAP.Vector2D[] doorPos { get; private set; }

        public ROOM()
        {
            leftTop = new MAP.Vector2D();
            rightBot = new MAP.Vector2D();
            Init();
        }
        public ROOM(Vector2D _leftTop, Vector2D _rightBot)
        {
            leftTop = _leftTop;
            rightBot = _rightBot;
            Init();
        }
        void Init()
        {
            passageInfo = new PASSAGE[4];
            doorPos = new MAP.Vector2D[4];
            for (int i = 0; i < 4; ++i)
            {
                passageInfo[i] = PASSAGE.NONE;
                doorPos[i] = new Vector2D(-1, -1);
            }
        }

        public bool CreateDoor(DIRECTION dir)
        {
            if (passageInfo[(int)dir] == PASSAGE.EXIST)
                return false;

            passageInfo[(int)dir] = PASSAGE.EXIST;
            int sizeX = rightBot.x - leftTop.x;
            int sizeY = rightBot.y - leftTop.y;
            switch (dir)
            {
                case DIRECTION.UP:
                    doorPos[(int)DIRECTION.UP].SetVector2D(leftTop.x + UnityEngine.Random.Range(1, sizeX), leftTop.y);
                    break;
                case DIRECTION.RIGHT:
                    doorPos[(int)DIRECTION.RIGHT].SetVector2D(leftTop.x, leftTop.y + UnityEngine.Random.Range(1, sizeY));
                    break;
                case DIRECTION.DOWN:
                    doorPos[(int)DIRECTION.DOWN].SetVector2D(leftTop.x + UnityEngine.Random.Range(1, sizeX), rightBot.y);
                    break;
                case DIRECTION.LEFT:
                    doorPos[(int)DIRECTION.LEFT].SetVector2D(rightBot.x, leftTop.y + UnityEngine.Random.Range(1, sizeY));
                    break;
            }
            return true;
        }

        public Vector2D GetCenterPosition() { return (leftTop + rightBot) / 2; }
        public Vector2D GetDoorPosition(DIRECTION dir) { return doorPos[(int)dir].NullClass() ? null : doorPos[(int)dir]; }
    }
}