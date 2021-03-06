using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;


struct WindowRect
{
	public int left;
	public int top;
	public int width;
	public int height;

	public WindowRect(int newLeft, int newTop, int newWidth, int newHeight)
	{
		this.left   = newLeft;
		this.top    = newTop;
		this.width  = newWidth;
		this.height = newHeight;
	}
}

struct Point
{
	public int x;
	public int y;

	public Point(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
}
namespace Tetris
{
	public class TetrisClass
	{
		const int    MAX_SPEED     = 9;
		const double DEFAULT_SPEED = 0.9; // 0.9 seconds

		const string msg1 = " Good job!! ";
		const string msg2 = " Press Key to start..";

		public static int Speed = 1; // Speed of the game.
		public static int Score = 0; // Total score.
		public static int Lines = 0; // Total lines completed.
		public static bool Cheer = false; //Have I ever Cheered?

		static Point ptBlock         = new Point();      // The x and y positions of the block.
		static WindowRect wrBlockAdj = new WindowRect(); // Block adjustment.
		static WindowRect PlayWindow = new WindowRect(); // The size of the field.    
		static bool isRows           = false;            // Is full row?
		public static bool isGameExit       = false;            // Terminate the game.
		public static bool isClear       = true;            // Terminate the game.

		static Game.StructBlock nextBlock = new Game.StructBlock();
		static Game.StructBlock currBlock = new Game.StructBlock();

		static ConsoleKeyInfo   kb;     // keyboard input
		static Game.TetrisClass Tetris;

		public void Run()
		{
			// Much better to use than GetTickCount because it's more precise.
			Stopwatch stopWatch = new Stopwatch();

			GameDesign();
			// PRESS ANY KEY TO START.
			Console.ForegroundColor = ConsoleColor.White;
			Console.SetCursorPosition((Console.WindowWidth - msg2.Length) / 2,
					PlayWindow.height+PlayWindow.top+2);
			Console.Write(msg2);
			Console.ResetColor();
			Console.ReadKey();

			// clear the message from the bottom window
			ClearBottomLine();

			Tetris = new Game.TetrisClass(PlayWindow);
			// Hook up the ProcessEvent.
			Tetris.ProcessEvent += new Game.TetrisHandler(Tetris_Process);

			ShowStatus();
			// Set up the next block
			ShowNextBlock();
			// Set up the first block
			PlayBlock(currBlock, true);

			// Start the game.
			stopWatch.Start();
			while (!isGameExit) // continue looping until escape key has been pressed.
			{
				TimeSpan ts      = stopWatch.Elapsed; 
				Point newPos     = ptBlock;           // Temporary variable for new position.
				bool isCanRotate = false;

				if(Console.KeyAvailable)
				{
					// get key input
					kb = Console.ReadKey(true); 

					switch(kb.Key)
					{
						case ConsoleKey.P:
						case ConsoleKey.Enter: // pause, resume
							// PRESS ANY KEY TO CONTINUE.
							Console.ForegroundColor = ConsoleColor.Magenta;
							Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -2 ));
							Console.Write("+---＼  +---+  |    |  ／--＼  +----");
							Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -1 ));
							Console.Write("|    |  |   |  |    |  |       |");
							Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -0 ));
							Console.Write("+---／  +---+  |    |  ＼--＼  +----");
							Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 +1 ));
							Console.Write("|       |   |  |    |       |  |");
							Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 +2 ));
							Console.Write("|       |   |  ＼__／  ＼--／  +----");
							Console.ResetColor();
							Console.ReadKey();
							// clear the message from the bottom window.
							Restart();
							break;
						case ConsoleKey.LeftArrow: // move left
							// Could go left?
							if(PlayWindow.left<newPos.x)
								newPos.x--;
							break;
						case ConsoleKey.RightArrow: // move top
							// Could go right?
							if((PlayWindow.width+PlayWindow.left)>(newPos.x+wrBlockAdj.width))
								newPos.x++; 
							break;                    
						case ConsoleKey.DownArrow: // move down 
							// Could go down?
							if((PlayWindow.height+PlayWindow.top)>(newPos.y+wrBlockAdj.height))
								newPos.y++; 
							break;
						case ConsoleKey.UpArrow:  // rotate
						case ConsoleKey.Spacebar: // rotate
							WindowRect newBlockAdj = new WindowRect();

							// Save old angle.
							Game.RotationEnum saveAngle = Tetris.Block.Angle;

							// try clockwise
							newBlockAdj = Tetris.Block.Rotate(Tetris.Block.getNextAngle(0));

							if(newPos.x+newBlockAdj.width>PlayWindow.width+PlayWindow.left)
								newPos.x = (PlayWindow.width+PlayWindow.left) - newBlockAdj.width;
							if(newPos.y+newBlockAdj.height>PlayWindow.height+PlayWindow.top)
								newPos.y = (PlayWindow.height+PlayWindow.top) - newBlockAdj.height;

							if(Tetris.IsCollided(new Point(newPos.x, newPos.y), newBlockAdj))
							{
								// try counter-clockwise
								newBlockAdj = Tetris.Block.Rotate(Tetris.Block.getNextAngle(1));

								if(newPos.x+newBlockAdj.width>PlayWindow.width+PlayWindow.left)
									newPos.x = (PlayWindow.width+PlayWindow.left) - newBlockAdj.width;
								if(newPos.y+newBlockAdj.height>PlayWindow.height+PlayWindow.top)
									newPos.x = (PlayWindow.height+PlayWindow.top) - newBlockAdj.height;

								if(Tetris.IsCollided(new Point(newPos.x, newPos.y), newBlockAdj))
									isCanRotate = false;
								else
									isCanRotate = true;
							}
							else
								isCanRotate = true;

							if(isCanRotate)
							{
								// can rotate, apply the new settings.
								ptBlock    = newPos;
								wrBlockAdj = newBlockAdj;
							}
							else
								// can't rotate, restore old angle.
								Tetris.Block.Rotate(saveAngle);
							break;
						case ConsoleKey.M:
							isGameExit=true;
							isClear=false;
							break;
					}

					if(!kb.Key.Equals(ConsoleKey.Spacebar))
						if(!Tetris.IsCollided(new Point(newPos.x, newPos.y), wrBlockAdj))
							ptBlock = newPos;

					if(ts.TotalSeconds<(DEFAULT_SPEED-(Speed-1)/10.0))
						Tetris.Block.Draw(ptBlock, wrBlockAdj, isCanRotate);
				}

				if(ts.TotalSeconds>=(DEFAULT_SPEED-(Speed-1)/10.0))
				{
					if((PlayWindow.height+PlayWindow.top)>(ptBlock.y+wrBlockAdj.height))
						if(Tetris.IsCollided(new Point (ptBlock.x, ptBlock.y+1), wrBlockAdj))
							// The block has collided, set the next block.
							PlayBlock(nextBlock, false);
						else
							// move down
							ptBlock.y++;
					else
						// Were at the bottom, set the next block.
						PlayBlock(nextBlock, false);

					Tetris.Block.Draw(ptBlock, wrBlockAdj, isCanRotate);
					stopWatch.Reset();
					stopWatch.Start();
				}
			}

			stopWatch.Stop();

			// Press any key to exit.
			Console.ForegroundColor = ConsoleColor.White;
			Console.ResetColor();
			Console.Clear();
		}

		private static void Tetris_Process(object o, Game.EventArgs e)
		{
			if(e.RowsCompleted>0)
			{
				isRows = true;
				Score += e.RowsCompleted*(e.RowsCompleted>1?15:10)*Speed;
				Lines += e.RowsCompleted;

				// Increase the speed according to the number of lines completed.
				if((Score>=10) && (Score<=25))
					Speed++;
				else if((Lines>=25) && (Score<=50))
					Speed++;
				else if((Score>=50) && (Score<=75))
					Speed++;
				else if((Score>=75) && (Score<=100))
					Speed++;
				else if((Score>=100) && (Score<=150))
					Speed++;
				else if((Score>=150) && (Score<=250))
					Speed++;
				else if((Score>=250) && (Score<=500))
					Speed++;
				else if(Score>=500)
					Speed++;

				ShowStatus();   
			}
		}

		private static void ShowStatus()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+3, 2);
			Console.Write("Score");
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+3, 5);
			Console.Write("Level");

			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, 3);
			Console.Write(String.Format("{0:D8}", Score));
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, 6);
			Console.Write(String.Format("{0:D8}", Speed));
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+3, 17);
			Console.Write("Pause : Enter or P");
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+3, 19);
			Console.Write("MENU :  M");

			if(!Cheer)
			{
				if(Score>=500)
				{
					Console.Clear();
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -2 ));
					Console.Write("／--＼  ／--＼  ／--＼  +---＼  ||");
					Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -1 ));
					Console.Write("|       |    |  |    |  |    |  ||");
					Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -0 ));
					Console.Write("|       |    |  |    |  |    |  ||");
					Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 +1 ));
					Console.Write("|   -+  |    |  |    |  |    |  ");
					Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 +2 ));
					Console.Write("＼-／|  ＼--／  ＼--／  +---／  []");
					Console.ResetColor();
					Console.ReadKey();
					Cheer=true;
					Restart();
				}

			}
		}

		private static void ShowNextBlock()
		{
			nextBlock = Tetris.Block.Generate(); // get next block

			Console.ForegroundColor = ConsoleColor.White;
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+4, 8);
			Console.Write("Next");
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, 9);
			Console.Write("+------+");

			for(int i=1; i<=6; i++)
			{
				Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, i+9);
				Console.Write("|      |");
			}

			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, 15);
			Console.Write("+------+");
			Console.ResetColor();

			Tetris.Block.Preview(new Point(PlayWindow.width+PlayWindow.left+6, 12), nextBlock);
		}

		private static void PlayBlock(Game.StructBlock sbBlock, Boolean isNew)
		{
			if(isNew)
				// create new block
				sbBlock = Tetris.Block.Generate();
			else
				Tetris.SendToField(ptBlock, wrBlockAdj);

			Tetris.Block.Assign(sbBlock);
			Tetris.Block.Build();
			Tetris.Block.Adjustment(ref wrBlockAdj);     

			ptBlock.x = (PlayWindow.left-wrBlockAdj.left) + (PlayWindow.width-wrBlockAdj.width)/2;
			ptBlock.y =  PlayWindow.top;

			Tetris.Block.Draw(ptBlock, wrBlockAdj, true);            
			ShowNextBlock();

			if(Tetris.IsCollided(ptBlock, wrBlockAdj))
			{
				Console.SetCursorPosition((Console.WindowWidth - msg1.Length) / 2,
						Console.WindowHeight/2);
				Console.Write(msg1);

				isGameExit = true;
			}
			else
				if(isRows)
				{
					isRows = false;
					Console.Beep(800,800);
				}
				else
				{
					Console.Beep();

				}
		}

		private static void GameDesign()
		{
			const string dsgnTB = "+--------------+";
			const string dsgnLR = "|              |";

			// Define window size.
			PlayWindow.left   = 33;
			PlayWindow.width  = dsgnTB.Length-2;
			PlayWindow.top    = 2;
			PlayWindow.height = 19;

			// hide the cursor
			Console.CursorVisible = false;

			// draw the top border
			Console.SetCursorPosition(PlayWindow.left-1, PlayWindow.top-1);
			Console.Write(dsgnTB);
			// draw the bottom border
			Console.SetCursorPosition(PlayWindow.left-1, PlayWindow.top+PlayWindow.height);
			Console.Write(dsgnTB);

			// draw the left and right border
			for(int i=PlayWindow.top; i<PlayWindow.height+PlayWindow.top ; i++)
			{
				Console.SetCursorPosition(PlayWindow.left-1, i);
				Console.Write(dsgnLR);
			}


		}

		private static void ClearBottomLine()
		{
			for(int i=1; i<Console.WindowWidth; i++)
			{
				Console.SetCursorPosition(i, PlayWindow.height+PlayWindow.top+2);
				Console.Write(" ");
			}
		}
		private static void Restart()
		{
			Console.Clear();
			ShowStatus();
			GameDesign();
			Console.ForegroundColor = ConsoleColor.White;
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+4, 8);
			Console.Write("Next");
			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, 9);
			Console.Write("+------+");

			for(int i=1; i<=6; i++)
			{
				Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, i+9);
				Console.Write("|      |");
			}

			Console.SetCursorPosition(PlayWindow.width+PlayWindow.left+2, 15);
			Console.Write("+------+");
			Console.ResetColor();

			Tetris.Block.Preview(new Point(PlayWindow.width+PlayWindow.left+6, 12), nextBlock);
		}
	}
}
namespace Game
{        
	enum RotationEnum
	{
		deg0   = 0,
		deg90  = 1,
		deg180 = 2,
		deg270 = 3
	}

	enum BlockTypeNum
	{
		block01 = 0,
		block02 = 1,
		block03 = 2,
		block04 = 3,
		block05 = 4,
		block06 = 5,
		block07 = 6
	}

	struct StructBlock
	{
		public RotationEnum angle;
		public BlockTypeNum type;

		public StructBlock(RotationEnum newAngle, BlockTypeNum newType)
		{
			this.angle = newAngle;
			this.type  = newType;
		}
	}

	struct StructBlockStyle
	{
		public ConsoleColor color;
		public Boolean      isBlock;

		public StructBlockStyle(ConsoleColor newColor, Boolean newIsBlock)
		{
			this.color   = newColor;
			this.isBlock = newIsBlock;
		}
	}

	public delegate void TetrisHandler(object o, EventArgs e);

	public class EventArgs
	{
		public readonly int RowsCompleted;

		public EventArgs(int r)
		{
			RowsCompleted = r;
		}
	}

	class BaseClass
	{
		protected static int BLOCK_SIZE = 4; // size of the block(4v4)

		protected static bool[] arrBlock         = new bool[BLOCK_SIZE<<2]; 
		protected static WindowRect TetrisField  = new WindowRect();    
		protected static Point       m_blockpos  = new Point();
		protected static StructBlock m_block     = new StructBlock();
		protected static StructBlockStyle[] arrField;
	}

	class BlockClass : BaseClass
	{
		public RotationEnum Angle
		{
			get
			{
				return m_block.angle;
			}
			set
			{
				m_block.angle = value;
			}
		}

		public BlockTypeNum Type
		{
			get 
			{
				return m_block.type;
			}
			set
			{
				m_block.type = value;
			}
		}

		public Point Location
		{
			get 
			{
				return new Point(m_blockpos.x, m_blockpos.y);
			}
			set
			{
				m_blockpos = value;
			}
		}   

		public int Size
		{
			get 
			{
				return BLOCK_SIZE;
			}
		}

		public ConsoleColor Color(BlockTypeNum typBlock)
		{
			// this function returns the color of the block.
			switch(typBlock)
			{
				case BlockTypeNum.block01:
					return ConsoleColor.Red;
				case BlockTypeNum.block02:
					return ConsoleColor.Blue;
				case BlockTypeNum.block03:
					return ConsoleColor.Cyan;
				case BlockTypeNum.block04:
					return ConsoleColor.Yellow;
				case BlockTypeNum.block05:
					return ConsoleColor.Green;
				case BlockTypeNum.block06:
					return ConsoleColor.Magenta;
				default:
					return ConsoleColor.DarkCyan;
			}
		}

		public StructBlock Generate()
		{
			Random rnd = new Random();

			// pick random pieces
			return new StructBlock((RotationEnum)rnd.Next(0, Enum.GetNames(typeof(RotationEnum)).Length),
					(BlockTypeNum)rnd.Next(0, Enum.GetNames(typeof(BlockTypeNum)).Length));
		}

		public WindowRect Rotate(RotationEnum newAngle)
		{
			WindowRect wrBlock = new WindowRect();

			Angle = newAngle;
			Build();
			Adjustment(ref wrBlock);

			return wrBlock;
		}

		public void Build()
		{
			// Get the data for the block.
			arrBlock = GetBlockData(new StructBlock(Angle, Type));
		}

		public Boolean[] GetBlockData(StructBlock structBlock)
		{
			// 0123
			// 4567
			// 8901
			// 2345

			// data for 4v4 block shapes
			bool[] arrData = new bool[BLOCK_SIZE<<2];

			switch(structBlock.type)
			{
				case BlockTypeNum.block01:
					if(structBlock.angle.Equals(RotationEnum.deg0) ||
							structBlock.angle.Equals(RotationEnum.deg180))
					{
						arrData[2]  = true; // ..#. 0123
						arrData[6]  = true; // ..#. 4567
						arrData[10] = true; // ..#. 8901
						arrData[14] = true; // ..#. 2345
					}
					else
					{
						arrData[12] = true; // .... 0123
						arrData[13] = true; // .... 4567
						arrData[14] = true; // .... 8901
						arrData[15] = true; // #### 2345
					}

					break;
				case BlockTypeNum.block02:
					arrData[0]  = true; // ##.. 0123
					arrData[1]  = true; // ##.. 4567
					arrData[4]  = true; // .... 8901
					arrData[5]  = true; // .... 2345
					break;
				case BlockTypeNum.block03:
					if(structBlock.angle.Equals(RotationEnum.deg0) ||
							structBlock.angle.Equals(RotationEnum.deg180))
					{
						arrData[5]  = true; // .... 0123
						arrData[6]  = true; // .##. 4567
						arrData[8]  = true; // ##.. 8901
						arrData[9]  = true; // .... 2345
					}
					else
					{
						arrData[1]  = true; // .#.. 0123
						arrData[5]  = true; // .##. 4567
						arrData[6]  = true; // ..#. 8901
						arrData[10] = true; // .... 2345
					}
					break;
				case BlockTypeNum.block04:
					if(structBlock.angle.Equals(RotationEnum.deg0) ||
							structBlock.angle.Equals(RotationEnum.deg180))
					{
						arrData[4]  = true; // .... 0123
						arrData[5]  = true; // ##.. 4567
						arrData[9]  = true; // .##. 8901
						arrData[10] = true; // .... 2345
					}
					else
					{
						arrData[2]  = true; // ..#. 0123
						arrData[5]  = true; // .##. 4567
						arrData[6]  = true; // .#.. 8901
						arrData[9]  = true; // .... 2345
					}
					break;
				case BlockTypeNum.block05:
					if(structBlock.angle.Equals(RotationEnum.deg0))
					{
						arrData[4]  = true; // .... 0123
						arrData[5]  = true; // ###. 4567
						arrData[6]  = true; // .#.. 8901
						arrData[9]  = true; // .... 2345
					}
					else if(structBlock.angle.Equals(RotationEnum.deg90))
					{
						arrData[1]  = true; // .#.. 0123
						arrData[4]  = true; // ##.. 4567
						arrData[5]  = true; // .#.. 8901
						arrData[9]  = true; // .... 2345
					}
					else if(structBlock.angle.Equals(RotationEnum.deg180))
					{
						arrData[5]  = true; // .... 0123
						arrData[8]  = true; // .#.. 4567
						arrData[9]  = true; // ###. 8901
						arrData[10] = true; // .... 2345
					}
					else
					{
						arrData[1]  = true; // .#.. 0123
						arrData[5]  = true; // .##. 4567
						arrData[6]  = true; // .#.. 8901
						arrData[9]  = true; // .... 2345
					}
					break;
				case BlockTypeNum.block06:
					if(structBlock.angle.Equals(RotationEnum.deg0))
					{
						arrData[4]  = true; // .... 0123
						arrData[5]  = true; // ###. 4567
						arrData[6]  = true; // #... 8901
						arrData[8]  = true; // .... 2345
					}
					else if(structBlock.angle.Equals(RotationEnum.deg90))
					{
						arrData[0]  = true; // ##.. 0123
						arrData[1]  = true; // .#.. 4567
						arrData[5]  = true; // .#.. 8901
						arrData[9]  = true; // .... 2345
					}
					else if(structBlock.angle.Equals(RotationEnum.deg180))
					{
						arrData[6]  = true; // .... 0123
						arrData[8]  = true; // ..#. 4567
						arrData[9]  = true; // ###. 8901
						arrData[10] = true; // .... 2345
					}
					else
					{
						arrData[1]  = true; // .#.. 0123
						arrData[5]  = true; // .#.. 4567
						arrData[9]  = true; // .##. 8901
						arrData[10] = true; // .... 2345
					}
					break;
				case BlockTypeNum.block07:
					if(structBlock.angle.Equals(RotationEnum.deg0))
					{
						arrData[4]  = true; // .... 0123
						arrData[5]  = true; // ###. 4567
						arrData[6]  = true; // ..#. 8901
						arrData[10] = true; // .... 2345
					}
					else if(structBlock.angle.Equals(RotationEnum.deg90))
					{    
						arrData[1]  = true; // .#.. 0123
						arrData[5]  = true; // .#.. 4567
						arrData[8]  = true; // ##.. 8901
						arrData[9]  = true; // .... 2345
					}
					else if(structBlock.angle.Equals(RotationEnum.deg180))
					{
						arrData[4]  = true; // .... 0123
						arrData[8]  = true; // #... 4567
						arrData[9]  = true; // ###. 8901
						arrData[10] = true; // .... 2345
					}
					else
					{
						arrData[1]  = true; // .##. 0123
						arrData[2]  = true; // .#.. 4567
						arrData[5]  = true; // .#.. 8901
						arrData[9]  = true; // .... 2345
					}
					break;
			}

			return arrData;
		}

		public void Adjustment(ref WindowRect wrBlock)
		{
			Adjustment(ref wrBlock, arrBlock);
		}

		public void Adjustment(ref WindowRect wrBlock, bool[] arrData)
		{
			//  This function returns the exact measurement of the block. 

			wrBlock = new WindowRect();

			int  col;
			int  row;
			bool isAdj;

			//  Check empty colums from the left-side of the block, and if found, 
			// increase the left margin.
			isAdj = true;
			for(col=0; col<BLOCK_SIZE; col++)
			{
				for(row=0; row<BLOCK_SIZE; row++)
					if(arrData[col+row*BLOCK_SIZE])
					{
						isAdj = false;
						break;
					}

				if(isAdj)
					// left margin
					wrBlock.left++;
				else
					break;
			}
			// end left adjustment

			//  Check empty rows from the top-side of the block, and if found, 
			// increse the top margin. 
			isAdj = true;
			for(row=0; row<BLOCK_SIZE; row++)
			{
				for(col=0; col<BLOCK_SIZE; col++)
					if(arrData[col+row*BLOCK_SIZE])
					{
						isAdj = false;
						break;
					}

				if(isAdj)
					wrBlock.top++;
				else
					break;
			}
			// end top adjustment

			//  Check empty columns from the right-side of the block, and if found, 
			// increase the right margin.
			isAdj = true;
			for(col=BLOCK_SIZE-1; col>=0; col--)
			{
				for(row=0; row<BLOCK_SIZE; row++)
					if(arrData[col+row*BLOCK_SIZE])
					{
						isAdj = false;
						break;
					}

				if(isAdj)
					wrBlock.width++;
				else
					break;
			}

			// get the exact width of the block
			wrBlock.width = BLOCK_SIZE - (wrBlock.left+wrBlock.width);
			// end right adjustment

			//  Check empty rows from the bottom-side of the block, and if found, 
			// increase the bottom.
			isAdj = true;
			for(row=BLOCK_SIZE-1; row>=0; row--)
			{
				for(col=0; col<BLOCK_SIZE; col++)
					if(arrData[col+row*BLOCK_SIZE])
					{
						isAdj = false;
						break;
					}

				if(isAdj)
					// bottom margin
					wrBlock.height++;
				else
					break;
			}

			// get the exact height of the block.
			wrBlock.height = BLOCK_SIZE - (wrBlock.top+wrBlock.height);
			// end top adjustment;
		}

		public void Draw(Point pt, WindowRect wrBlockAdj, Boolean isRotateUpdate)
		{
			// Draw the block.
			if(!Location.x.Equals(pt.x) || !Location.y.Equals(pt.y) || isRotateUpdate)
			{
				TetrisClass.DrawField(pt, wrBlockAdj);
				Console.BackgroundColor = Color(Type);
				for(int row=wrBlockAdj.top; row<wrBlockAdj.top+wrBlockAdj.height; row++)
					for(int col=wrBlockAdj.left; col<wrBlockAdj.left+wrBlockAdj.width; col++)
						if(arrBlock[col+row*BLOCK_SIZE])  
						{
							Console.SetCursorPosition(pt.x+col-wrBlockAdj.left, pt.y+row-wrBlockAdj.top);
							Console.Write(" ");//Draw the Playing Block
						}
				Console.ResetColor();

				Location = pt;
			}
		}

		public void Preview(Point pt, StructBlock structBlock)
		{
			// shows a preview of a block
			WindowRect wrBlockAdj = new WindowRect();
			bool[] arrData        = GetBlockData(structBlock);

			//  retrieve the exact measurement of the block
			// so we can able to draw the block in correct position.
			Adjustment(ref wrBlockAdj, arrData);

			Console.BackgroundColor = Color(structBlock.type);
			for(int row=wrBlockAdj.top; row<wrBlockAdj.top+wrBlockAdj.height; row++)
				for(int col=wrBlockAdj.left; col<wrBlockAdj.left+wrBlockAdj.width; col++)
					if(arrData[col+row*BLOCK_SIZE])  
					{
						Console.SetCursorPosition(pt.x+col-wrBlockAdj.left-wrBlockAdj.width/2, 
								pt.y+row-wrBlockAdj.top-wrBlockAdj.height/2);
						Console.Write(" ");//Draw the Next Block
					}
			Console.ResetColor();
		}

		public RotationEnum getNextAngle(int rotateOption)
		{
			if(rotateOption.Equals(0))
				// clockwise
				switch(Angle)
				{
					case RotationEnum.deg0:
						return RotationEnum.deg90;
					case RotationEnum.deg90:
						return RotationEnum.deg180;
					case RotationEnum.deg180:
						return RotationEnum.deg270;
					default:
						return RotationEnum.deg0;
				}
			else
				// counter-clockwise
				switch(Angle)
				{
					case RotationEnum.deg0:
						return RotationEnum.deg270;
					case RotationEnum.deg270:
						return RotationEnum.deg180;
					case RotationEnum.deg180:
						return RotationEnum.deg90;
					default:
						return RotationEnum.deg0;
				}
		}

		public void Assign(StructBlock sbNew)
		{
			Angle = sbNew.angle;
			Type  = sbNew.type;   
		}
	}

	class TetrisClass : BaseClass
	{
		public event TetrisHandler ProcessEvent;

		public BlockClass Block = new BlockClass();

		public TetrisClass(WindowRect wrField)
		{
			TetrisField = wrField;
			BuildField();
		}

		public void BuildField()
		{
			arrField = new StructBlockStyle[TetrisField.width*
				TetrisField.height];
		}

		public static void DrawField(Point pt, WindowRect wrBlockAdj)
		{
			int w = TetrisField.width;
			int h = TetrisField.height;

			for(int row=0; row<h; row++)
				for(int col=0; col<w; col++)
				{
					if(((StructBlockStyle)arrField[col+row*w]).isBlock)
					{
						Console.BackgroundColor = ((StructBlockStyle)arrField[col+row*w]).color;
						Console.SetCursorPosition(TetrisField.left+col, TetrisField.top+row);
						Console.Write(" ");//Draw the Block that put
						Console.ResetColor();
					}
					else
					{
						Console.SetCursorPosition(TetrisField.left+col, TetrisField.top+row);
						Console.Write(" ");
					}
				}

			Console.ResetColor();
		}

		public bool IsCollided(Point pt, WindowRect wrBlockAdj)
		{
			int sx = pt.x - TetrisField.left;
			int sy = pt.y - TetrisField.top;
			int w  = TetrisField.width;

			int blockIndex;
			int fieldIndex;

			for(int row=0; row<wrBlockAdj.height; row++)
				for(int col=0; col<wrBlockAdj.width; col++)
				{
					blockIndex = (wrBlockAdj.left+col)+((wrBlockAdj.top+row)*BLOCK_SIZE);
					fieldIndex = ((sx+sy*w)+col)+row*w;

					if(arrBlock[blockIndex] && ((StructBlockStyle)arrField[fieldIndex]).isBlock)
						return true;
				}

			return false;
		}

		public void SendToField(Point pt, WindowRect wrBlockAdj)
		{
			// This function sends the block data to field.
			int blockIndex;
			int fieldIndex;

			for(int row=0; row<wrBlockAdj.height; row++)
				for(int col=0; col<wrBlockAdj.width; col++)
				{
					blockIndex = (wrBlockAdj.left+col)+
						(wrBlockAdj.top+row)*
						Block.Size;
					fieldIndex = (pt.x-TetrisField.left+col)+
						(pt.y-TetrisField.top+row)*
						TetrisField.width;

					if(arrBlock[blockIndex])
						arrField[fieldIndex] = new StructBlockStyle(Block.Color(Block.Type), true);
				}

			ProcessRows();
		}

		public void ProcessRows()
		{
			// This function check to see if rows were completed.
			int w           = TetrisField.width;
			int h           = TetrisField.height;
			int rowCounter  = h-1;
			int rowTotal    = 0;
			bool isFullLine = true;

			// Store rows that are not completed.
			StructBlockStyle[] arrData = new StructBlockStyle[TetrisField.width*
				TetrisField.height];

			for(int row=h-1; row>=0; row--)
			{
				for(int col=w-1; (col>=0) && isFullLine; col--)
					if(!((StructBlockStyle)arrField[col+row*w]).isBlock)
						isFullLine = false;

				if(!isFullLine)
				{
					// copy the row
					for(int col=w-1; col>=0; col--)
						arrData[col+rowCounter*w] = arrField[col+row*w];

					rowCounter--;
					isFullLine = true;
				}
				else
					// Do not include rows that are completed.
					rowTotal++;
			}

			// get all the rows that are not completed.
			arrField = arrData; 

			EventArgs e = new EventArgs(rowTotal);
			RaiseEvent((object)this, e);
		}

		private void RaiseEvent(object o, EventArgs e)
		{
			if (ProcessEvent != null)
				ProcessEvent(o, e);
		}

	}
}
namespace Main
{
	public class Data : System.IComparable
	{
		public int data{get;set;}
		public string name{get;set;}
		public int CompareTo(object obj)
		{
			return this.data.CompareTo(((Data)obj).data);
		}
	}
	public class MainClass
	{

		public static Tetris.TetrisClass gameMain;
		static void Main(string[] args)
		{
			Console.Title="";
			Console.Clear();
			gameMain = new 	Tetris.TetrisClass();
			bool end=false;
			int selected=1;
			int isEnter=0;
			string PlayerName="";
			Data[] Data=new Data[0];
			try{
				var dexmlSerializer = new XmlSerializer(typeof(Data[]));
				var xmlSettings = new System.Xml.XmlReaderSettings()
				{
					CheckCharacters = false,
				};
				using (var streamReader = new StreamReader("Data", Encoding.UTF8))
					using (var xmlReader
							= System.Xml.XmlReader.Create(streamReader, xmlSettings))
					{
						Data = (Data[])dexmlSerializer.Deserialize(xmlReader);
					}
			}catch{
				Data=new Data[0];
			}
			string startmsg="Press any key to start...";
			string msg1="Start";
			string msg2="Level up!!";
			string msg3="Level down!!";
			string msg4="Quit";
			Console.CursorVisible = false;
			Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -2 ));
			Console.Write("---+--- +---- ---+--- +---＼ -+- ／--＼");
			Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 -1 ));
			Console.Write("   |    |        |    |    |  |  |");
			Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2  ));
			Console.Write("   |    +----    |    +---／  |  ＼--＼");
			Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 +1 ));
			Console.Write("   |    |        |    |＼     |       |");
			Console.SetCursorPosition((Console.WindowWidth ) / 2 - 17,(Console.WindowHeight / 2 +2 ));
			Console.Write("   |    +----    |    |  ＼  -+- ＼--／");
			Console.SetCursorPosition((Console.WindowWidth - startmsg.Length) / 2,
					Console.WindowHeight-2);
			Console.Write(startmsg);
			Console.ReadKey();
			Console.Clear();
			Console.CursorVisible = true;
			Console.SetCursorPosition(0,Console.WindowHeight-3);
			Console.Write("Please write your name\n\n");
			PlayerName=Console.ReadLine();
			Console.CursorVisible = false;
			Console.Clear();
			while (!end)
			{
				while(isEnter==0)
				{
					Console.CursorVisible = false;
					Console.BackgroundColor= ConsoleColor.Black;
					Console.SetCursorPosition((Console.WindowWidth-17)/2, Console.WindowHeight / 2-6);
					Console.Write("+----------------+");

					for(int i=1; i<=12; i++)
					{
						Console.SetCursorPosition((Console.WindowWidth-17)/2, Console.WindowHeight / 2+i-6);
						Console.Write("|                |");
					}

					Console.SetCursorPosition((Console.WindowWidth-17)/2, Console.WindowHeight / 2+6);
					Console.Write("+----------------+");
					Console.ResetColor();

					Console.SetCursorPosition((Console.WindowWidth -msg1.Length) / 2,Console.WindowHeight / 2-4);
					if(selected==1)
					{
						Console.BackgroundColor = ConsoleColor.White;
						Console.ForegroundColor = ConsoleColor.Black;
					}
					Console.Write(msg1);
					Console.ResetColor();
					Console.SetCursorPosition((Console.WindowWidth -msg2.Length) / 2,(Console.WindowHeight / 2 -2));
					if(selected==2)
					{
						Console.BackgroundColor = ConsoleColor.White;
						Console.ForegroundColor = ConsoleColor.Black;
					}
					Console.Write(msg2);
					Console.ResetColor();
					Console.ForegroundColor = ConsoleColor.White;
					Console.SetCursorPosition((Console.WindowWidth -msg3.Length) / 2,(Console.WindowHeight / 2  ));
					if(selected==3)
					{
						Console.BackgroundColor = ConsoleColor.White;
						Console.ForegroundColor = ConsoleColor.Black;
					}
					Console.Write(msg3);
					Console.ResetColor();
					Console.SetCursorPosition((Console.WindowWidth -msg4.Length) / 2,(Console.WindowHeight / 2 +2));
					if(selected==4)
					{
						Console.BackgroundColor = ConsoleColor.White;
						Console.ForegroundColor = ConsoleColor.Black;
					}
					Console.Write(msg4);
					Console.ResetColor();
					Console.SetCursorPosition((Console.WindowWidth) / 2 -4,(Console.WindowHeight / 2 +4));
					Console.Write("LEVEL       ");
					Console.SetCursorPosition((Console.WindowWidth) / 2 +3,(Console.WindowHeight / 2 +4));
					Console.Write(Tetris.TetrisClass.Speed);
					ConsoleKeyInfo key = Console.ReadKey(true); 
					switch(key.Key)
					{
						case ConsoleKey.Enter:
							if(selected==1){
								Tetris.TetrisClass.isGameExit=false;
								Tetris.TetrisClass.Score=0;
								Tetris.TetrisClass.Lines=0;
								Tetris.TetrisClass.Cheer=false;
								Tetris.TetrisClass.isClear=true;
								isEnter=1;
								Console.Clear();
								gameMain.Run();
								if(Tetris.TetrisClass.isClear)
								{
									int tmplength = Data.Length;
									Array.Resize(ref Data,Data.Length+1);
									Data[tmplength]=new Data();
									Data[tmplength].data=Tetris.TetrisClass.Score;
									Data[tmplength].name=PlayerName;
									Array.Sort(Data);
									Array.Reverse(Data);
									Console.Clear();
									using (var streamWriter = new StreamWriter("Data", false, Encoding.UTF8))
									{
										var xmlSerializer1 = new XmlSerializer(typeof(Data[]));
										xmlSerializer1.Serialize(streamWriter, Data);
									}
								}
								Console.SetCursorPosition(Console.WindowWidth/2+10,Console.WindowHeight/2-8);
								Console.Write("現在のスコア : {0}",Tetris.TetrisClass.Score);
								for(int i=0;i<Data.Length;i++)
								{
									if(0<=i && i<= Data.Length-1)
									{
										Console.SetCursorPosition(Console.WindowWidth/2+10,Console.WindowHeight/2-5+2*i);
										Console.Write("{0}位: {1} point ({2})",i+1,Data[i].data,Data[i].name);
									}
									if(i>5)
										break;
								}
							}
							if(selected==2){
								if(Tetris.TetrisClass.Speed!=9)
									Tetris.TetrisClass.Speed++;
							}
							if(selected==3){
								if(Tetris.TetrisClass.Speed!=1)
									Tetris.TetrisClass.Speed--;
							}
							if(selected==4){
								end=true;
								isEnter=1;
							}
							break;
						case ConsoleKey.DownArrow:
							if(selected!=4)
								selected++;
							break;
						case ConsoleKey.UpArrow:
							if(selected!=1)
								selected--;
							break;
						default:
							break;
					}
				}
				isEnter=0;
			}
			Console.ResetColor();
			Console.Clear();
			Console.CursorVisible = true;
		}
	}
}
