import { TableContainer, Paper, Table, TableBody, TableRow, TableCell } from '@mui/material';
import { useAppSelector } from '../../app/store/configureStore';

// interface Props {
//     subtotal?: number;
// }

export default function BasketSummary( /* {subtotal} : Props */ ) {
    const { basket } = useAppSelector(state => state.basket);
    
    console.log(basket)
    
    const deliveryFee = basket?.totalPrice !== undefined ? Number.parseFloat(basket.totalPrice) > 100 ? 0 : (basket?.items.length || 10) * 10 : 20;
    const total = Math.round(((Number(basket?.totalPrice) || 0) + deliveryFee) * 100) / 100;

    return (
        <>
            <TableContainer component={Paper} variant={'outlined'}>
                <Table>
                    <TableBody>
                        <TableRow>
                            <TableCell colSpan={2}>Subtotal</TableCell>
                            <TableCell align="right">${basket?.totalPrice}</TableCell>
                        </TableRow>
                        <TableRow>
                            <TableCell colSpan={2}>Delivery fee*</TableCell>
                            <TableCell align="right">${deliveryFee}</TableCell>
                        </TableRow>
                        <TableRow>
                            <TableCell colSpan={2}>Total</TableCell>
                            <TableCell align="right">   {total}</TableCell>
                        </TableRow>
                        <TableRow>
                            <TableCell>
                                <span style={{fontStyle: 'italic'}}>*Orders over $100 qualify for free delivery</span>
                            </TableCell>
                        </TableRow>
                    </TableBody>
                </Table>
            </TableContainer>
        </>
    )
}