module FSPowerPowerExample

open System

let v  = vector [1.0;1.0;1.0] + vector [2.0;2.0;2.0] // (3.0; 3.0; 3.0)
let c = complex 0.0 1.0 * complex 0.0 1.0 // -1r+0i
let r = (1N/2N) * (1N/3N) // 1/6