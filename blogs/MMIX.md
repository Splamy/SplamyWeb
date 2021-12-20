# From IL to MMIX

Writing a C# to MMIX AOT Compiler, because... why not.

## Why ???

The IEEE Student Group at my University hosts a fun little advent calender where instead of chocolate you get a coding problem each day.
Unlike at the popular Advent of Code however you don't submit your precalculated solution, but your source code. From then on it will be compiled in a VM and unit tested with various input. And depending on how many cases it correctly solves you get points.  
So far so good. But the fun starts when they announced that they had over 25 languages supported on the backed and if you can solve each task with a different language, you get a little bonus.  

Sounds like a challange to me. Now, unlike most high-level languages, where even with little time investment you can get up to speed pretty fast, writing assembly is tedious.  
So why not taking a little detour and write a quick-and-dirty compiler myself...
[img](after all, why not) (xkcd time investment for automation)

## So, what's MMIX ?
MMIX is an assembly language for a theoretical cpu architecture, designed by Donald Knuth.
The instructions are fixed 4 bytes wide with 1 byte operand and 3 opcode dependant data. With that specification it belongs to the CISC category. All that on top of a 64bit addres size memory.  
The interesting part is how the architecture handles registers. Instead of like most common architectures with general purpose registers and a (call-)stack, MMIX only has a stack, and the top 255 octets on the stack *are* the registers. This means calling and returning from functions simply shifts your 255 register wide 'viewing window' up and down on stack.

## The Idea
What would be the fastest way to get a simple compiler working.
Desiging a custom language most definitely is out of question because of all the extra work creating a grammar, lexer, parsing and transforming into processable operations.  
So something like a intermediate, preprocessed language would be nice, and there are a few that come to my mind. LLVM with it's IR, Java with the JVM Bytecode and C#, or rather the .NET ecosystem where also languages like VB.NET or F# compile to: IL. And since C#'s my favourite language, the choice's obvious.

### IL
Functions in IL simply described works like a stack.

## Implementation

- Memory layout
- Allocation
- Array, String layout

## Problems

- Literally everything
    - Bad stack management / Running out of registers
    - Object inheritance
    - Attributes
    - Garbage Collection
    - Reflection lol
    - Minimal binary size (Tree Shaking?)