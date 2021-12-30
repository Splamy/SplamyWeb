# From IL to MMIX

Writing a C# to MMIX AOT Compiler, because... why not.

## No, really, why ???

The IEEE Student Group at my University hosts a fun little advent calender where instead of chocolate you get a coding problem each day. (If you're reading this during December check it out [here](https://advent.ieee.uni-passau.de/advent))  
Unlike at the popular Advent of Code however you don't submit your precalculated solution, but your source code. From then on it will be compiled in a VM and unit tested with various input. And depending on how many cases it correctly solves you get points.  
So far so good. But the fun starts when they announced that they had over 25 languages supported on the backed and if you can solve each task with a different language, you get a little bonus.

Sounds like a challenge to me. Now, unlike most high-level languages, where even with little time investment you can get up to speed pretty fast, writing assembly is tedious.  
So why not taking a little detour and write a quick-and-dirty compiler myself...  
[![Automation: expectations vs reality](https://imgs.xkcd.com/comics/automation.png)](https://xkcd.com/1319/)

## So, what's MMIX ?
[MMIX](http://mmix.cs.hm.edu/index.html) is an assembly language for a theoretical cpu architecture, designed by Donald Knuth.
The instructions are fixed 4 bytes wide with 1 byte operand and 3 opcode dependant data. With that specification it belongs to the RISC category. All that on top of a 64bit address size memory.  
The interesting part is how the architecture handles registers. Instead of like most common architectures with general purpose registers and a (call-)stack, MMIX only has a stack, and the top 255 octets on the stack *are* the registers. This means calling and returning from functions simply shifts your 255 register wide 'viewing window' up and down on stack.

## The Idea
What would be the fastest way to get a simple compiler working?  
Designing a custom language most definitely is out of question because of all the extra work creating a grammar, lexer, parsing and transforming into processable operations.  
So something like a intermediate, preprocessed language would be nice, and there are a few that come to my mind. LLVM with it's IR, Java with the JVM Bytecode and C#, or rather the .NET ecosystem where also languages like VB.NET or F# compile to: IL. And since C#'s my favorite language, the choice's obvious.

### IL
Functions in IL simply described work like a stack. So you push data on the stack, then push an operation on the stack and it will consume as many elements as it needs and leave its result on the stack.  
A simple example:

```cil
ldc.i4 42 // puts 42 on the stack
ldc.i4 1  // puts 1 on the stack
add       // pops the 42 and 1 from the stack and leaves 43 on the stack
```

Usually all VM's and a lot of compilers transform such intermediate stack logic into a [SSA](https://en.wikipedia.org/wiki/Static_single_assignment_form).  
And - spoiler alert - as we will see later, there is a reason for that. The code produced by a literal translation is anything but efficient or well optimizable.  
But, yeah my plan wasn't to make it the 'normal way'. I wanted the most straight forward way to implement it: An IL compiler that takes the stack logic and 1:1 translates it into machine code.

## Implementation



### Memory Layout

Memory layout was pretty simple. While MMIX has 4 predefined regions `TEXT`, `DATA`, `POOL` and `STACK` and some rough guidance on how to use them, we can pretty much do whatever we want.  
Nonetheless, let's be nice citizens and put compiled code into `TEXT`.  
`DATA` is where our dynamically allocated data will be.  
Static fields and constants dive into the `POOL`.  
And last but not least the `STACK`. This is the only one that is predefined by MMIX how it's used.  
Easy enough. Next.

### Allocation
Usually you wouldn't think twice about allocating, just write `new` and you're done. But it's not that simple in assembly. We have a big chunk of memory and can write wherever we want, so now it's our job to create some order. Or simply said, we need our own `malloc`.  
Once again I went for the (almost) simplest solution. A doubly-linked list of memory blocks.  
Why only almost? Because the simplest solution would be to just keep allocating block after block and never free anything. That would be possible, but might use too much memory in some cases.  
The doubly-linked list has a nice versatility to it:
1. Freeing an object is an O(1) operation in every case.  
2. If we need want to burst allocate, we can just cache the current end and append each allocation.  
3. If we want to allocate memory-efficient we can iterate though the list and find the first free region that is big enough at the tradeoff that allocating is now a O(n) operation.  

A doubly-linked list, where each node stores the size of the block and a pointer to the previous and next node.  
Simple enough:
```csharp
public unsafe struct MemAllocNode
{
	public MemAllocNode* prev;
	public MemAllocNode* next;
	public ulong size;
}
```

We store the size in each node just for case 3. so we can squeeze new allocations in between old allocations:  
![memory layout](https://share.splamy.de/21/12/memory.svg)

For C# speaking, pointers to structs might look weird, since that's basically the same as a class without all that `*`-stuff.  
But in this case a struct has multiple advantages:
- Reading and writing is by value and not though a pointer indirection.  
  This means `var foo = bar;` copies all values instead of just the pointer.
- Being a struct means we can work with it on the stack and don't have to allocate an object inbefore.  
  (Which is useful when you are writing a `malloc` function to allocate in the first hand, duh!)
- It's easy to to get a pointer to a struct in C#, which we will need to write to arbitrary addresses.  
  With classes we'd have a really bad time.

### Arrays and Strings

Ever noticed that `string`s are basically just `char[]`s? Yeah, me too, that's why I'm implementing both pretty much identically.  
For an array `T[]` we just need 2 things: the size of the array and the pointer to the first element.  
The only question is the memory layout. And here we have 2 choices:  
![Array layout](https://share.splamy.de/21/12/struct.svg)

A) is as far as I know the way .NET and CoreCLR do it. And usually probably preferable since it's very cheap to pass and copy around. But it's a bit more work to implement and you have to be very careful handling that extra data at `ptr-8`.  
B) on the other hand has the nice property of easily creating subslices of arrays. As well as another special advantage for MMIX. Some trap (=syscall) instructions, like reading stdin or writing stdout, require a pointer to struct with the buffer size and the buffer pointer.
This makes B) a very worthwhile choice.

## How's it performing?

<table>
<tr><td>C#</td><td>IL</td><td>MMIX</td></tr>
<tr>
<td>

```csharp
int Add(int a, int b) {
	return a + b;
}
```

</td>
<td>

```cil
ldarg.0
ldarg.1
add
ret
```

</td>
<td>

```mmix
Add
 SET $2,$1
 SET $1,$0
 % IL_0000: ldarg.0
 SET $3,$1
 % IL_0001: ldarg.1
 SET $4,$2
 % IL_0002: add
 ADD $3,$3,$4
 % IL_0003: ret
 SET $0,$3
 POP 1,0
```

</td>
<td>
</tr>
</table>

Handwritten MMIX:
```mmix
C.Add(Int32, Int32)
 ADD $0,$0,$1
 POP 1,0
```

.NET Core:
```x86
C.Add(Int32, Int32)
    L0000: lea eax, [rdx+r8]
    L0004: ret
```



## Problems

Ok, so where does this all break apart?  
Uhm,... everywhere.

### The bad stack management
Using the building stack from IL is a really bad idea in MMIX. Because when you run out of your 255 registers in the current stack, there's nothing you can do.  
This is where ugly subfunction hacks would have to start. And at this point it would be faster to just do it the correct way.

### Object inheritance
The classes in the current implementation have no runtime information about their type.  
This means basically that one of the core concepts of C#, object oriented programming, will not work at all.  
No `is`, no `as`, no `(T)` casts.

### Reflection
No runtime type information, no runtime reflection. And there's a lot of stuff that depends on reflection in C#.
Dependency injection? Forget it. Serialization? No chance (or not until we get some good C#10 source generators).

### Attributes
Closely tied with reflection: Attributes. Yeah you might guess it, also not in scope for this project.

### .NET Standard library
Probably the biggest loss of all. I can't use anything of the standard library.  
And you don't even know you are using it until you write `10.ToString()` and realize how much code in the background is needed to run this.  
The problem here is basically the same that the .NET Core team had to solve for their [binary trimming](https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained).
You want to throw away as much unneeded code as possible, but that's hard when basically anything could be loaded with reflection.  
Another problem is that even few used functions can include a lot of dependencies.
There is a reason self-contained C# binaries are so huge even with trimming and compression.

### Garbage collection
And last but not least: Garbage collection.  
Yeah, it might be a bit clunky to write `free(...)` in C#, but it saves me from writing a full GC.

# Conclusion

All in all it was a very fun project, that I wouldn't do again.  
Hope you enjoyed it.
