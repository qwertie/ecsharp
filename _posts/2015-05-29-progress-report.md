---
title: Annual Progress Report
layout: post
#commentIssueId: 7
---
It's been a disappointing school year. I returned to university in the hope that finally, _finally_ I'd be able to devote a lot of time to Loyc, make real progress, and perhaps finally make something that would attract the interest of ... well, anyone. Actually getting a Master's degree was never the point, it was just a bonus.

Sadly, it's been a bit of a disaster. Three of the four courses I took were pretty difficult. I managed to get an A in 521 Functional Programming and 619 Quantum Computing, but I was unable to keep up in Category Theory and I had to withdraw. The thing is, it really wasn't the actual subject matter that made these courses difficult, it was much more about the ability of the professors to teach the subject. As a highly abstract subject, Category Theory, in particular, requires either a student with a degree in mathematics (since existing textbooks are, IMO, virtually incomprehensible to non-mathematicians like me) or a professor with excellent teaching skills and a willingness to step outside the perspective and notational conventions of hardcore mathematics, and describe things in a way that a mere software engineer like me can understand. Emphatically, my professor has neither of these qualities. Not long before I withdrew from the class, I asked him to explain the meaning of a notation he kept using. Twice. He refused, saying it would "hold up the class". Class after class, I simply couldn't figure out what the hell he was talking about. The same professor taught Functional Programming, a central component of which was the lambda (λ) calculus. He spent weeks talking about the λ-calculus, but I swear to God (should he exist) I could explain everyhing you need to know about λ-calculus to any ordinary programmer in a couple of hours... not counting exercises. Cuz you have to do exercises if you really want to learn something. Anyway, he spent two full lectures (2.5 hours) giving what seemed an incomprehensible proof of the confluence of the λ-calculus; luckily knowledge of the proof turned out not to be required, but I wasted time studying it just the same.

Of course, when I'm only taking one or two courses, I can always get an A, but it means burning through a lot of time, time I can't spend doing anything useful. Time I can't spend designing the future of programming.

Then, at Christmas, my supervisor told me he wouldn't sponsor or supervise my plan to create Enhanced C#. Why not? Well, I gave him a short presentation about it, and he simply didn't understand it. You see, he's a Haskell guy. A lambda calculus guy. A category theorist. He doesn't use C, or C++, or C#, or Go, or Rust, or LISP, or anything like that. He's merely in charge of the university's Programming Language Lab, and as far as I know, the only language he uses is Haskell. Occasionally, anyway--more often he seems to be working on proofs and LaTeX documents. He's never programmed in C#, so he said he wasn't equipped to judge something called "Enhanced C#" that has a "LISP-inspired macro system". And therefore he wouldn't supervise me for that project. Instead, he insisted, I should be working on _his_ research: MPL.

How did I get in this pickle? Well, a little over a year ago it occurred to me that maybe grad students get paid to be TAs (Teaching Assistants). Perhaps, then, it was possible to return to uni, and be cash flow neutral while working on something that mattered to me. I wanted to stay in Calgary, though, because I have a house and I live with my best friend, who refuses to move. That meant I'd have to go to the UCalgary. But UCalgary only has one solitary professor that works in the field of programming language design, a Dr. Cockett. I knew in advance that we might have some friction, because I knew his communication skills were below average, but here's what he said when I contacted him about being my supervisor:

> I do remember you!  A very ORIGINAL student! Your project looks pretty darn good! And the basic idea for what you want to do interesting ...
...
> If you want to consider this I am happy to "supervise" you ... I will let you be very independent!  

Okay, so that was encouraging, and I signed up. But, it turns out he has zero interest in my research, and I don't get to be very independent. 

What he wants me to work on is a concurrent research language called MPL. 

What is MPL? Well, don't ask Dr. Cockett. The first thing he wanted me to do was to (try to) read a thesis by a former student named Subashis. 

Let me give you a taste of his work. First, after a general introduction that we can safely skip, the thesis spends a little time talking about the "sequential" part of the language, which, by the way, has little importance because it's basically a small subset of Haskell with a bunch of letter "c"s thrown in. But here's the first code example in the thesis:

    1  -- data definitions
    2  data Nat -> c = Zero : c
    3                  Succ : c -> c
    4  data List a -> c = Nil : c
    5                     Cons : a -> c -> c
    6  
    7  -- length function given by fold combinator
    8  fold
    9    length : List a -> Nat
    10   length x by x =
    11     Nil -> Zero
    12     Cons y ys -> Succ (length ys)

What does this mean? Well, if you know λ-calculus you'll recognize this as a definition of _Natural numbers_ in a sort of _church numeral_ form (where `Succ` represents "plus one" so `Succ Zero` means one and `Succ (Succ Zero)` means two, and -1 does not exist), and lists. I'm not sure if the language actually supports numbers of any kind (if not, the author was wise enough not to come right out and say so--that wouldn't help him get a Master's degree)... hence the church numerals. `length` is a function that computes the length of a list, and the meaning of "fold" is introduced as follows:

> To compute the length of a list it is necessary to recurse over the list: MPL only allows disciplined recursion using "folding" (or catamorphisms), hence the program is introduced with the `fold` command: this is an example of a fold function definition.

> In line 9, the name of the function, length and type of the function `List a->Nat` are given separated by ":". Thus, `length` has type `List a->Nat` which means, explicitly, the function `length` takes a list of arbitrary type as input and produces the length of the list as a unary natural number. The types `List a` and `Nat` are defined in line 1 to 5 in Figure 2.1 using data declarations. The data declarations and the recursive definition using the `fold` construct are all sequential constructs of the MPL language. The sequential world of MPL is discussed further in chapter 3.

Enlightening, right?! But what about all the "c"s everywhere? What is the meaning of "x by x"? Sorry, the thesis doesn't really address that. But the real purpose of MPL is concurrency, so let's see how the thesis introduces the concurrent part of MPL, shall we?

> An example of a concurrent program in which two processes are plugged together is given in Figure 2.2. A simple process, `TwoWayTalk` is given in line 5-17. It uses the `drive` construct which allows recursive definitions in the concurrent world, analogous to the `fold` in the sequential world. For a fuller description see Chapter 4. This process receives messages on each of its output channels, namely y and z, and passes these messages as a pair on its input channel, x. Then the process takes a message on its input channel, x, and sends a copy of the message on each of the output channels, y and z. The type of a channel or protocol is determined by the actions possible on a channel: the protocol, in this case, is defined in line 1-3. We wish to plug this process to another process which we describe next. The process, Average has one input channel w and one output channel x. It recieves a pair of numbers on channel x, and then sends the average on channel w: the average function is assumed to have been defined elsewhere. The process `Average` then recieves a message on w and pass it on to x.

> As the input channel of `TwoWayTalk` and the output channel of `Average` have the same protocol, these two processes can be plugged together to make a compsite process `TwoAverage`. In line 34-37, `plug` command is used to plug `Average` to `TwoWayTalk` on channel `x`. Finally one runs the process `TwoAverage`.

1  -- protocol definition
2  protocol Talk (a b) => $C =
3         #response :: get a (put b $C) => $C
4 
5  -- process definitions
6  drive
7    TwoWayTalk :: () Talk ((a * b) b) => Talk (a c), Talk (b c)
8    TwoWayTalk x => y, z by x =
9          #response: #response on y
10                    #response on z
11                    get a1 on y.
12                    get a2 on z.
13                    put (a1 * a2) on x
14                    get b on x.
15                    put b on y
16                    put b on z
17                    call TwoWayTalk x => y, z
18
19 drive
20   Average :: () Talk (a b) => Talk ((a * b) c)
21   Average w => x by w =
22              #response: #response on x;
23                         get a on x.
24                         case a of
25                              (a1 * a2) -> put (average a1 a2) on w
26                                           get a on w.
27                                           put a on x
28                                           call Average w => x
29
30 -- This process is a composite process via
31 -- plugging of two above processes
32 TwoAverage :: () Talk ((a * b) c) => Talk (a b), Talk (a b)
33 TwoAverage w => y, z =
34           plug on x
35                call Average w => x
36                to
37                call TwoWayTalk x => y, z
38
39 -- initializing command to start the execution of a MPL program
40 run TwoAverage

Uh-huh. Now you might be able to figure out what this example is meant to do if you study it carefully for long enough. Even if you figure out how this works, though, you won't learn much about MPL. And I'm sure you don't have time to read the rest, so just take it from me, you're just not going to understand MPL from reading this thesis.

And that's really the central problem that I've been dealing with since I got here. You might think, what does a poorly-written thesis have to do with Dr. Cockett? Simple: Subashis and Dr. Cockett have equal amounts of teaching skill. While they have different expository styles, both of them use lots of words without ever bringing simplicity or clarity to a subject, confuse listeners and readers by leaving out key facts, and are unskilled at syntax design (not surprising, since designing a language is partly a pedagogical activity: it's about communicating clearly with humans, not just machines).

But hey, couldn't I learn MPL by playing with its compiler? Ahh... no. I was told that the compiler was of poor quality and never really finished. And I didn't know how to build it anyway.

Fundamentally, I suspect Dr. Cockett's problem is that he doesn't really believe teaching is his responsibility. He is merely a source of information; he needs only to dryly state the theory, and a student that applies himself will (eventually) learn the material; therefore, he's fulfilled his responsibility. After all, I myself _did_ figure out MPL eventually. Doesn't that mean he taught it well enough? Well, no. Hell no.

In the last several months we've had weekly or biweekly meetings about MPL, and each week he (or sometimes Jonathan, a mathematician and longtime student of Dr. Cockett) would talk for two to four hours about some small piece of MPL or something related to MPL. Yet I spent the whole time fairly baffled, never getting good answers to questions such as:

1. What are the goals of the language (without making references to Category Theory)?
2. What are all the concurrent primitives of the language (without making references to Category Theory)?
3. What do these primitives do (without making references to Category Theory)?
4. What is the _purpose_ of each primitive? What do you use them for and when?
5. How do you write a nontrivial parallel algorithm in MPL? You know, something that dynamically changes its process graph...?
6. How do you write large-scale realistic programs in MPL?
7. How do you write "Hello, World!" in MPL?
8. Can you give me an example of a complex protocol (one that shows off the power of the MPL type system)?

The fact that I found Functional Programming and Category Theory challenging is no surprise; those courses are both taught by Dr. Cockett. But at least in those courses, Dr. Cockett had years of experience teaching them; when it came to the language he himself designed, he had very little experience.

Finally in the middle of this month we made serious progress. I learned some shocking facts...

- There was not, and never had been, a plan for how to talk to a terminal (stdout). A "Hello World" program had never been written.
- The new parser developed for MPL over the last few months didn't support integers or other literals.
- Jonathan, who often says he likes to think of MPL as an operating system, was himself not understand some of the details of MPL.

And there's another shocking fact I've known for some time, which is that nobody here is familiar with (the rather large body of) other academic literature about concurrency. Nor is anyone here an expert in concurrent algorithms, or traditional approaches to concurrency.

But at long last I think I have it figured out. So, I think in my next post I will explain once and for all what MPL is (as I see it), why it is useful, how to use it, and what's wrong with it.

In the meantime, I'm told I have to turn in an "Annual Progress Report" to the university. Sigh. There's not much to report.

## Student Achievements

_Other Funding Held During This Reporting Period:_
Other than what? Anyway, I believe the answer is "None."

_Research:_
Around Christmas day or so, Dr. Cockett declined to supervise me for my proposed research topic and instead asked me to work on his research language, called MPL. I have spent the last few months trying unsuccessfully to understand MPL. However, about a week ago I made a breakthrough and I feel now that I have a good enough understanding of MPL that I could explain all the key points to anyone else in less than an hour. I have now started playing with ideas for implementing MPL.

_Publications:_
None (although I've written multiple articles online about the work that Dr. Cockett won't supervise).

_Conferences:_
None

_Teaching:_
I taught CPSC 217 in the fall, and played more of an "assistant" role CPSC 403 in the winter.

_Report on Current Year's Plan for Professional Development:_
Well, I am hoping to improve my teaching skills as I teach kids to code on Saturdays at the CPL. I attended a couple of GradSkills workshops too.

_Plans for Upcoming Year:_
In the near future I hope to write a program that will perform type checking and translate MPL code to a "production" programming language. Further plans are not yet established.

_Plans for Professional Development for Upcoming Year:_
None
