# SPSA trajectory

- base ruleset : `v7-d8-spsa-base`
- iterations   : 20
- games/eval   : 100
- c (perturb)  : 40
- a (gain)     : 8
- seed0        : 20260425
- tunable      : d,s,c,l,x,b,a,u
- pinned       : m=100
- bounds       : [10, 2000]
- guard-rail   : reject iteration if Unfin > 25%
- objective    : f(theta) = (PartyShareOfDecisive - 0.5)^2 * 10000

theta_0 = `d:300 s:350 c:900 l:200 x:400 b:250 a:400 u:300 m:100`

## Iteration 0

- delta: `[+1,+1,+1,-1,-1,+1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:340 s:390 c:940 l:160 x:360 b:290 a:360 u:340 m:100` -> y+ = 1681.92  (P%=9.0%, Unfin=11.0%, Dec=89.0%, Median plies=156)
- theta- `d:260 s:310 c:860 l:240 x:440 b:210 a:440 u:260 m:100` -> y- = 485.89  (P%=28.0%, Unfin=7.0%, Dec=93.0%, Median plies=183)
- theta_after: `d:180 s:230 c:780 l:320 x:520 b:130 a:520 u:180 m:100`
- iteration elapsed: 562.4s

## Iteration 1

- delta: `[+1,-1,+1,+1,-1,-1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:220 s:190 c:820 l:360 x:480 b:90 a:480 u:220 m:100` -> y+ = 459.18  (P%=28.6%, Unfin=9.0%, Dec=91.0%, Median plies=164)
- theta- `d:140 s:270 c:740 l:280 x:560 b:170 a:560 u:140 m:100` -> y- = 289.72  (P%=33.0%, Unfin=6.0%, Dec=94.0%, Median plies=182)
- theta_after: `d:163 s:247 c:763 l:303 x:537 b:147 a:537 u:163 m:100`
- iteration elapsed: 595.5s

## Iteration 2

- delta: `[-1,+1,-1,+1,-1,+1,-1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:123 s:287 c:723 l:343 x:497 b:187 a:497 u:123 m:100` -> y+ = 654.41  (P%=24.4%, Unfin=14.0%, Dec=86.0%, Median plies=160)
- theta- `d:203 s:207 c:803 l:263 x:577 b:107 a:577 u:203 m:100` -> y- = 544.44  (P%=26.7%, Unfin=10.0%, Dec=90.0%, Median plies=177)
- theta_after: `d:174 s:236 c:774 l:292 x:548 b:136 a:548 u:174 m:100`
- iteration elapsed: 625.5s

## Iteration 3

- delta: `[-1,+1,+1,-1,-1,+1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:134 s:276 c:814 l:252 x:508 b:176 a:508 u:214 m:100` -> y+ = 277.78  (P%=33.3%, Unfin=13.0%, Dec=87.0%, Median plies=192)
- theta- `d:214 s:196 c:734 l:332 x:588 b:96 a:588 u:134 m:100` -> y- = 277.78  (P%=33.3%, Unfin=4.0%, Dec=96.0%, Median plies=167)
- theta_after: `d:174 s:236 c:774 l:292 x:548 b:136 a:548 u:174 m:100`
- iteration elapsed: 603.4s

## Iteration 4

- delta: `[+1,-1,+1,-1,-1,-1,+1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:214 s:196 c:814 l:252 x:508 b:96 a:588 u:214 m:100` -> y+ = 530.55  (P%=27.0%, Unfin=11.0%, Dec=89.0%, Median plies=168)
- theta- `d:134 s:276 c:734 l:332 x:588 b:176 a:508 u:134 m:100` -> y- = 212.67  (P%=35.4%, Unfin=4.0%, Dec=96.0%, Median plies=190)
- theta_after: `d:142 s:268 c:742 l:324 x:580 b:168 a:516 u:142 m:100`
- iteration elapsed: 639.7s

## Iteration 5

- delta: `[+1,+1,-1,-1,+1,-1,-1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:182 s:308 c:702 l:284 x:620 b:128 a:476 u:102 m:100` -> y+ = 71.01  (P%=41.6%, Unfin=11.0%, Dec=89.0%, Median plies=200)
- theta- `d:102 s:228 c:782 l:364 x:540 b:208 a:556 u:182 m:100` -> y- = 820.92  (P%=21.3%, Unfin=11.0%, Dec=89.0%, Median plies=164)
- theta_after: `d:217 s:343 c:667 l:249 x:655 b:93 a:441 u:67 m:100`
- iteration elapsed: 621.7s

## Iteration 6

- delta: `[-1,+1,+1,-1,-1,-1,-1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:177 s:383 c:707 l:209 x:615 b:53 a:401 u:27 m:100` -> y+ = 303.31  (P%=32.6%, Unfin=11.0%, Dec=89.0%, Median plies=204)
- theta- `d:257 s:303 c:627 l:289 x:695 b:133 a:481 u:107 m:100` -> y- = 4.53  (P%=47.9%, Unfin=6.0%, Dec=94.0%, Median plies=211)
- theta_after: `d:247 s:313 c:637 l:279 x:685 b:123 a:471 u:97 m:100`
- iteration elapsed: 680.5s

## Iteration 7

- delta: `[-1,+1,+1,+1,-1,+1,-1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:207 s:353 c:677 l:319 x:645 b:163 a:431 u:57 m:100` -> y+ = 60.49  (P%=42.2%, Unfin=10.0%, Dec=90.0%, Median plies=201)
- theta- `d:287 s:273 c:597 l:239 x:725 b:83 a:511 u:137 m:100` -> y- = 55.45  (P%=42.6%, Unfin=6.0%, Dec=94.0%, Median plies=186)
- theta_after: `d:248 s:312 c:636 l:278 x:686 b:122 a:472 u:98 m:100`
- iteration elapsed: 635.0s

## Iteration 8

- delta: `[-1,-1,-1,+1,-1,-1,+1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:208 s:272 c:596 l:318 x:646 b:82 a:512 u:58 m:100` -> y+ = 36.53  (P%=44.0%, Unfin=9.0%, Dec=91.0%, Median plies=190)
- theta- `d:288 s:352 c:676 l:238 x:726 b:162 a:432 u:138 m:100` -> y- = 40.74  (P%=43.6%, Unfin=6.0%, Dec=94.0%, Median plies=194)
- theta_after: `d:248 s:312 c:636 l:278 x:686 b:122 a:472 u:98 m:100`
- iteration elapsed: 628.5s

## Iteration 9

- delta: `[-1,+1,-1,+1,-1,-1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:208 s:352 c:596 l:318 x:646 b:82 a:432 u:138 m:100` -> y+ = 129.13  (P%=38.6%, Unfin=12.0%, Dec=88.0%, Median plies=178)
- theta- `d:288 s:272 c:676 l:238 x:726 b:162 a:512 u:58 m:100` -> y- = 0.27  (P%=49.5%, Unfin=3.0%, Dec=97.0%, Median plies=214)
- theta_after: `d:261 s:299 c:649 l:265 x:699 b:135 a:485 u:85 m:100`
- iteration elapsed: 631.6s

## Iteration 10

- delta: `[+1,-1,-1,-1,+1,+1,+1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:301 s:259 c:609 l:225 x:739 b:175 a:525 u:45 m:100` -> y+ = 29.54  (P%=44.6%, Unfin=8.0%, Dec=92.0%, Median plies=201)
- theta- `d:221 s:339 c:689 l:305 x:659 b:95 a:445 u:125 m:100` -> y- = 30.86  (P%=44.4%, Unfin=10.0%, Dec=90.0%, Median plies=186)
- theta_after: `d:261 s:299 c:649 l:265 x:699 b:135 a:485 u:85 m:100`
- iteration elapsed: 636.2s

## Iteration 11

- delta: `[-1,+1,-1,+1,-1,-1,+1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:221 s:339 c:609 l:305 x:659 b:95 a:525 u:125 m:100` -> y+ = 1.23  (P%=48.9%, Unfin=10.0%, Dec=90.0%, Median plies=179)
- theta- `d:301 s:259 c:689 l:225 x:739 b:175 a:445 u:45 m:100` -> y- = 4.73  (P%=52.2%, Unfin=8.0%, Dec=92.0%, Median plies=204)
- theta_after: `d:261 s:299 c:649 l:265 x:699 b:135 a:485 u:85 m:100`
- iteration elapsed: 674.9s

## Iteration 12

- delta: `[-1,-1,+1,+1,-1,-1,+1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:221 s:259 c:689 l:305 x:659 b:95 a:525 u:45 m:100` -> y+ = 129.13  (P%=38.6%, Unfin=12.0%, Dec=88.0%, Median plies=185)
- theta- `d:301 s:339 c:609 l:225 x:739 b:175 a:445 u:125 m:100` -> y- = 30.86  (P%=55.6%, Unfin=10.0%, Dec=90.0%, Median plies=205)
- theta_after: `d:271 s:309 c:639 l:255 x:709 b:145 a:475 u:95 m:100`
- iteration elapsed: 701.2s

## Iteration 13

- delta: `[-1,+1,-1,-1,-1,+1,+1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:231 s:349 c:599 l:215 x:669 b:185 a:515 u:55 m:100` -> y+ = 188.68  (P%=63.7%, Unfin=9.0%, Dec=91.0%, Median plies=201)
- theta- `d:311 s:269 c:679 l:295 x:749 b:105 a:435 u:135 m:100` -> y- = 199.67  (P%=35.9%, Unfin=8.0%, Dec=92.0%, Median plies=196)
- theta_after: `d:270 s:310 c:638 l:254 x:708 b:146 a:476 u:94 m:100`
- iteration elapsed: 673.0s

## Iteration 14

- delta: `[-1,-1,+1,+1,+1,-1,-1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:230 s:270 c:678 l:294 x:748 b:106 a:436 u:54 m:100` -> y+ = 100.00  (P%=40.0%, Unfin=10.0%, Dec=90.0%, Median plies=190)
- theta- `d:310 s:350 c:598 l:214 x:668 b:186 a:516 u:134 m:100` -> y- = 4.73  (P%=52.2%, Unfin=8.0%, Dec=92.0%, Median plies=204)
- theta_after: `d:280 s:320 c:628 l:244 x:698 b:156 a:486 u:104 m:100`
- iteration elapsed: 633.6s

## Iteration 15

- delta: `[+1,+1,+1,-1,-1,-1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:320 s:360 c:668 l:204 x:658 b:116 a:446 u:144 m:100` -> y+ = 183.04  (P%=36.5%, Unfin=15.0%, Dec=85.0%, Median plies=181)
- theta- `d:240 s:280 c:588 l:284 x:738 b:196 a:526 u:64 m:100` -> y- = 60.49  (P%=42.2%, Unfin=10.0%, Dec=90.0%, Median plies=223)
- theta_after: `d:268 s:308 c:616 l:256 x:710 b:168 a:498 u:92 m:100`
- iteration elapsed: 716.2s

## Iteration 16

- delta: `[+1,-1,-1,-1,-1,+1,+1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:308 s:268 c:576 l:216 x:670 b:208 a:538 u:52 m:100` -> y+ = 104.60  (P%=39.8%, Unfin=12.0%, Dec=88.0%, Median plies=196)
- theta- `d:228 s:348 c:656 l:296 x:750 b:128 a:458 u:132 m:100` -> y- = 8.26  (P%=47.1%, Unfin=13.0%, Dec=87.0%, Median plies=187)
- theta_after: `d:258 s:318 c:626 l:266 x:720 b:158 a:488 u:102 m:100`
- iteration elapsed: 713.4s

## Iteration 17

- delta: `[+1,+1,-1,-1,-1,-1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:298 s:358 c:586 l:226 x:680 b:118 a:448 u:142 m:100` -> y+ = 14.79  (P%=53.8%, Unfin=9.0%, Dec=91.0%, Median plies=203)
- theta- `d:218 s:278 c:666 l:306 x:760 b:198 a:528 u:62 m:100` -> y- = 46.81  (P%=43.2%, Unfin=5.0%, Dec=95.0%, Median plies=197)
- theta_after: `d:261 s:321 c:623 l:263 x:717 b:155 a:485 u:105 m:100`
- iteration elapsed: 616.8s

## Iteration 18

- delta: `[-1,+1,-1,-1,-1,+1,-1,-1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:221 s:361 c:583 l:223 x:677 b:195 a:445 u:65 m:100` -> y+ = 22.68  (P%=45.2%, Unfin=16.0%, Dec=84.0%, Median plies=216)
- theta- `d:301 s:281 c:663 l:303 x:757 b:115 a:525 u:145 m:100` -> y- = 0.00  (P%=50.0%, Unfin=10.0%, Dec=90.0%, Median plies=200)
- theta_after: `d:263 s:319 c:625 l:265 x:719 b:153 a:487 u:107 m:100`
- iteration elapsed: 816.9s

## Iteration 19

- delta: `[+1,-1,+1,-1,+1,-1,-1,+1]`  (for letters d,s,c,l,x,b,a,u)
- theta+ `d:303 s:279 c:665 l:225 x:759 b:113 a:447 u:147 m:100` -> y+ = 1.23  (P%=48.9%, Unfin=10.0%, Dec=90.0%, Median plies=205)
- theta- `d:223 s:359 c:585 l:305 x:679 b:193 a:527 u:67 m:100` -> y- = 1.23  (P%=48.9%, Unfin=10.0%, Dec=90.0%, Median plies=202)
- theta_after: `d:263 s:319 c:625 l:265 x:719 b:153 a:487 u:107 m:100`
- iteration elapsed: 683.1s

## Summary

- iterations    : 20
- rejections    : 0  (guard-rail trips)
- elapsed       : 218.2 min
- best y        : 0.00  (0.500 / 0.500 P%(dec) distance from 50%)
- best at       : iter18-minus
- best theta    : `d:301 s:281 c:663 l:303 x:757 b:115 a:525 u:145 m:100`
- final theta   : `d:263 s:319 c:625 l:265 x:719 b:153 a:487 u:107 m:100`
