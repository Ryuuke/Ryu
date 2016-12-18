# Ryu
A programming language I made for learning purpose only.

###WIP

Supported language features: 

- Functions, variable length function arguments

    ```c++
square :: (number: s64) -> s64 {
      return number * number;
}

my_print :: (a_string: str, ...) {
      // Implementation
}
    ```
- C-like pointers

 ```c++
my_pointer := new s64; // 0
value := *my_pointer; // Get pointer value
pointer_address := &my_pointer; // Get pointer address
    ```
- New, Delete, Defer keywords
 
 ```c++
 player := new Player
  defer delete player; // Will be deleted later at the end of the current scope
  ```
- Static arrays

 ```c++
 my_array := [100] s32;
  ```
- Dynamic arrays

 ```c++
 dynamic_array := [..] s32;
  ```
- Enums & type casting
    ```c++
const one := 1;

Days :: enum {
	MONDAY = one, // 1
	TUESDAY, // 2 
	WEDNESDAY = one + 2, // 3
}

two := #cast(Days.TUESDAY, s32); // 2
```
- Structs, with in-place field initialization

  ```c++
  Player :: struct {
	age : s32 = 0;
	name : str = "Ryuuke";
  }
  ```

- Type system with type inference

  ```c++
  my_int : s32 = 5;
  my_infered_int := 5; // same
  my_float := 5.5; // f32
  my_large_float := 5.5f64;
  my_str := "hello";
  my_char := 'a'
  my_function_ptr : (n: s32) -> s32; // function pointer declaration
  is_true := true;
  int_ptr : ^int = null;
  ```
  
- Statements
  ```c++
  if condition {
      do_something();
  }
  else {
      dont();
  }
  
  for index: 0..20 {
    do_something(index);
  }
  
  for item: array {
    do_something(item);
  }
  ```
- Constants

 ```c++
 const PI := 3.14;
  ```
  
Front-end compiler features:
 - language Parser
 - AST generation
 - Symbole table
 - Type Inference
 - Type Checking
 - Typing error
 
LLVM based back-end compiler, assembly code generation work is in very early stage:
 - A sample for building a compiled 'hello world' x86 program exists
  
  ```c++
  #load "io.ryu"

main :: () {
      println("Hello world !");
}
  ```
